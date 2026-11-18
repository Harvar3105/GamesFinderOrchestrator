using GamesFinder.Orchestrator.API.Controllers.Contracts.Games;
using GamesFinder.Orchestrator.Domain.Classes;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Contracts;
using GamesFinder.Orchestrator.Domain.Enums;
using GamesFinder.Orchestrator.Domain.Interfaces.DomainServices;
using GamesFinder.Orchestrator.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace GamesFinder.Orchestrator.API.Controllers.Games;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
  private readonly ILogger<GameController> _logger;
  private readonly IGameRepository _repo;
  private readonly IGamesWithOffersService _gamesWithOffersService;
  private readonly IMemoryCache _cache;
  private readonly CacheOptions _cacheOptions;

  // Tags change only when the Steam worker stores new games, so a stale list for a while is acceptable.
  private const string TagsCacheKey = "games:tags";

  public GameController(
    ILogger<GameController> logger,
    IGameRepository repo,
    IGamesWithOffersService gamesWithOffers,
    IMemoryCache cache,
    CacheOptions cacheOptions)
  {
    this._logger = logger;
    this._repo = repo;
    this._gamesWithOffersService = gamesWithOffers;
    this._cache = cache;
    this._cacheOptions = cacheOptions;
  }

  // All tags present on stored games, sorted alphabetically. Used by the frontend to build the tags filter.
  [HttpGet("tags")]
  public async Task<IActionResult> GetTagsAsync()
  {
    try
    {
      var tags = await _cache.GetOrCreateAsync(TagsCacheKey, entry =>
      {
        entry.AbsoluteExpirationRelativeToNow = _cacheOptions.TagsDuration;
        return _repo.GetDistinctTagsAsync();
      });
      return Ok(new {Tags = tags});
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving distinct game tags");
      return Problem("An error occurred while processing your request.");
    }
  }

  [HttpGet("checkGameExistsByName")]
  public async Task<IActionResult> CheckExistingGameByNameAsync(string gameName)
  {
    try
    {
      var exists = await _repo.ExistsByAppNameAsync(gameName);
      return Ok(new {Exists = exists});
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error checking existence of game by name: {gameName}");
      return Problem("An error occurred while processing your request.");
    }
  }

  [HttpGet("checkGameExists")]
  public async Task<IActionResult> CheckGameExists(string gameId)
  {
    var success = Guid.TryParse(gameId, out var parsedId);
    if (!success) return BadRequest("Invalid id requested.");

    try
    {
      var exists = await _repo.ExistsAsync(parsedId);
      return Ok(new {Exists = exists});
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error checking existence of game: {gameId}");
      return Problem("An error occurred while processing your request.");
    }
  }

  [HttpGet("checkGameExistsBySteamId")]
  public async Task<IActionResult> CheckGameExistsBySteamId(int steamId)
  {
    try
    {
      var exists = await _repo.ExistsBySteamIdAsync(steamId);
      return Ok(new {Exists = exists});
    } catch (Exception ex)
    {
      _logger.LogError(ex, $"Error checking existence of game by steam id: {steamId}");
      return Problem("An error occurred while processing your request.");
    }
  }

  [HttpGet("getGameIdBySteamId")]
  public async Task<IActionResult> GetGameIdBySteamIdAsync(int steamId)
  {
    try
    {
      var gameId = await _repo.GetIdBySteamIdAsync(steamId);
      if (gameId is null) return NotFound($"Game not found.");
      return Ok(new {Id = gameId});
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error checking existence of Steam ID: {steamId}");
      return Problem("An error occurred while processing your request.");
    }
  }

  [HttpGet("getGame")]
  public async Task<IActionResult> GetGame(string gameId)
  {
    if (string.IsNullOrEmpty(gameId)) return BadRequest("gameId was not provided.");
    var success = Guid.TryParse(gameId, out var parsedId);
    if (!success) return BadRequest("⚠️Invalid gameId format. Must be a valid GUID.)");

    try
    {
      var game = await _repo.GetByIdAsync(parsedId);
      if (game is null) return NotFound("Not found");
      return Ok(new {Game = game});
    } catch (Exception ex)
    {
      _logger.LogError(ex, $"Error retrieving game with ID: {gameId}");
      return Problem("An error occurred while processing your request.");
    }
  }

  // Case-insensitive partial match by name. Only games, without offers.
  // TODO: Measure performance on production-sized data and set limits (max pageSize, min name length).
  [HttpGet("searchByName")]
  public async Task<ActionResult<SearchGamesByNameResponse>> SearchByNameAsync([FromQuery] SearchGamesByNameRequest request)
  {
    try
    {
      var namePart = request.Name.Trim();
      var totalGamesCount = await _repo.CountByNameAsync(namePart);
      if (totalGamesCount == 0) return NotFound("Not found");

      var games = await _repo.SearchByNameAsync(namePart, request.Page, request.PageSize);
      return Ok(new SearchGamesByNameResponse {Games = games, TotalGamesCount = totalGamesCount});
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error searching games by name: {request.Name}. Page: {request.Page}, PageSize: {request.PageSize}");
      return Problem("An error occurred while processing your request.");
    }
  }

  // Only games, without offers.
  [HttpGet("getPaged")]
  public async Task<IActionResult> GetPaged(int page, int pageSize){
    if (pageSize <= 0 || page < 1) return BadRequest("⚠️Invalid data provided");

    var games = await _repo.GetPagedAsync(page, pageSize);
    var totalGamesCount = await _repo.GetTotalGamesCountAsync();
    return Ok(new {Games = games, TotalGamesCount = totalGamesCount});
  }

  // Games with lowest prices. No offers
  [HttpGet("getPagedWithLowest")]
  public async Task<IActionResult> GetPagedWithLowest(int page, int pageSize, ECurrency currency = ECurrency.USD)
  {
    if (pageSize <= 0 || page < 1) return BadRequest("⚠️Invalid data provided");

    try
    {
      var games = await _gamesWithOffersService.GetPagedWithLowestAsync(page, pageSize, currency);
      var totalGamesCount = await _repo.GetTotalGamesCountAsync();
      return Ok(new {Games = ToLowestPriceResponse(games), TotalGamesCount = totalGamesCount});
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error retrieving paged games with lowest prices. Page: {page}, PageSize: {pageSize}");
      return Problem("An error occurred while processing your request.");
    }
  }

  // Filtered games with lowest prices. No offers
  [HttpGet("GetPagedWithLowestFiltered")]
  public async Task<IActionResult> GetPagedWithLowestFiltered(
    int page,
    int pageSize,
    [FromQuery] GamesFilter filter,
    ECurrency currency = ECurrency.USD
    )
  {
    if (pageSize <= 0 || page < 1) return BadRequest("⚠️Invalid data provided");
    if (filter.Min < 0 || filter.Max < 1 || filter.Min >= filter.Max) return BadRequest("Provided invalid boundries.");

    try
    {
      var games = await _gamesWithOffersService.GetPagedWithLowestFilteredAsync(page, pageSize, filter, currency);
      var totalGamesCount = await _gamesWithOffersService.GetTotalCountFilteredAsync(filter, currency);
      return Ok(new {Games = ToLowestPriceResponse(games), TotalGamesCount = totalGamesCount});
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error retrieving paged filtered games with lowest prices. Page: {page}, PageSize: {pageSize}");
      return Problem("An error occurred while processing your request.");
    }
  }

  // System.Text.Json ignores tuple fields (Item1, Item2), so tuples are mapped to named objects.
  private static IEnumerable<object> ToLowestPriceResponse(IEnumerable<(Game Game, decimal? LowestPrice)> games)
  {
    return games.Select(g => new {g.Game, g.LowestPrice});
  }
}