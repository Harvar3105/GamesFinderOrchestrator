using GamesFinder.Orchestrator.Domain.Interfaces.Repositories;
using GamesFinder.Orchestrator.API.Controllers.Contracts.Steam;
using GamesFinder.Orchestrator.Domain.Classes.DTOs;
using GamesFinder.Orchestrator.Domain.Enums;
using GamesFinder.Orchestrator.Domain.Interfaces.DomainServices;
using GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamesFinder.Orchestrator.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SteamController : ControllerBase
{
  private readonly ILogger<SteamController> _logger;
  private readonly ISteamService _steamService;
  private readonly IGameRepository _gamesRepo;
  private readonly IGameOfferRepository _offersRepo;
  private readonly IGamesWithOffersService _gamesWithOffersService;

  public SteamController(ILogger<SteamController> logger, ISteamService steamService, IGameRepository gamesRepository, IGameOfferRepository offersRepository, IGamesWithOffersService gamesWithOffersService)
  {
    _logger = logger;
    _steamService = steamService;
    _gamesRepo = gamesRepository;
    _offersRepo = offersRepository;
    _gamesWithOffersService = gamesWithOffersService;
  }

  [HttpPost("scrap")]
  [Authorize(Policy = "DevPolicy")]
  public async Task<IActionResult> ScrapSteamIdsAsync([FromBody] SteamRequestModel model)
  {
    try
    {
      await _steamService.PublishIdsScrapeTaskAsync(model.steamIds, model.updateExistingGames, model.updateExistingOffers);
      return Ok(new { Message = $"✅Scraping task initiated for {model.steamIds.Count} Steam IDs. Take a break, process will take some time 😎" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error initiating scraping task for Steam IDs.");
      return StatusCode(500, "An error occurred while processing your request.");
    }
  }

  [HttpGet("checkGameExistsByName")]
  public async Task<IActionResult> CheckExistingGameByNameAsync(string gameName)
  {
    try
    {
      _logger.LogInformation($"Checking existence of game by name: {gameName}");
      var game = await _gamesRepo.GetByAppNameAsync(gameName);
      if (game == null) return Ok(new {Exists = false});
      else {
        _logger.LogInformation($"Game found Steam ID: {game.SteamID}");
        return Ok(new {Exists = true, GameId = game.Id, SteamId = game.SteamID });
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error checking existence of game by name: {gameName}");
      return StatusCode(500, "An error occurred while processing your request.");
    }
  }

  [HttpGet("checkGameExists")]
  public async Task<IActionResult> CheckExistingSteamIdAsync(int steamId, bool getGame = false)
  {
    try
    {
      var exists = await _gamesRepo.ExistsBySteamIdAsync(steamId);
      if (getGame && exists)
      {
        var game = await _gamesRepo.GetBySteamIdAsync(steamId);
        return Ok(new { Exists = true, Game = game!.Id });
      }
      return Ok(new {Exists = exists});
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error checking existence of Steam ID: {steamId}");
      return StatusCode(500, "An error occurred while processing your request.");
    }
  }

  [HttpGet("getGameIdBySteamId")]
  public async Task<IActionResult> GetGameIdBySteamIdAsync(int steamId)
  {
    try
    {
      bool exists = await _gamesRepo.ExistsBySteamIdAsync(steamId);
      if (exists) {
        var gameId = await _gamesRepo.GetIdBySteamIdAsync(steamId);
        return Ok(new {Id = gameId});
      }
      else {
        return NotFound(new { Message = $"Game with Steam ID {steamId} not found." });
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error checking existence of Steam ID: {steamId}"); return StatusCode(500, "An error occurred while processing your request.");
    }
  }

  [HttpGet("checkGameOfferExists")]
  public async Task<IActionResult> CheckExistingGameOfferAsync(int steamId)
  {
    try
    {
      var exists = await _offersRepo.ExistsByVendorsIdAsync(steamId.ToString());
      return Ok(new { Exists = exists });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error checking existence of game offer for Steam ID: {steamId}");
      return StatusCode(500, "An error occurred while processing your request.");
    }
  }

  [HttpGet("getOfferId")]
  public async Task<IActionResult> GetOfferId(string? gameId, int? steamId)
  {
    if ((string.IsNullOrEmpty(gameId) && steamId == null) ||
    (!string.IsNullOrEmpty(gameId) && steamId != null))
    {
      return BadRequest("⚠️Provide either gameId or steamId, but not both.");
    }

    try
    {
      if (!string.IsNullOrEmpty(gameId))
      {
        bool success = Guid.TryParse(gameId, out Guid parsedGameId);
        if (!success) return BadRequest("⚠️Invalid gameId format. Must be a valid GUID.");
        var id = await _offersRepo.GetIdByGameIdAsync(parsedGameId, EVendor.Steam);
        return Ok(new { OfferId = id });
      }
      else
      {
        var id = await _offersRepo.GetIdByVendorsGameIdAsync(steamId!.ToString()!, EVendor.Steam);
        return Ok(new { OfferId = id });
      }
    } catch (Exception ex)
    {
      _logger.LogError(ex, $"Error retrieving offer ID for gameId: {gameId} or steamId: {steamId}");
      return StatusCode(500, "An error occurred while processing your request.");
    }
  }

  [HttpGet("getPagedWithCurrency")]
  public async Task<IActionResult> GetPagedWithCurrency(
    int page,
    int pageSize,
    string? currency,
    [FromQuery] SteamFiltersRequestModel filters = default!
  )
  {
    if (pageSize <= 0 || page < 0) return BadRequest("⚠️Invalid data provided");
    var parsedCurrency = ECurrencyHelpers.GetECurrency(currency!);

    var paginationFilter = new PaginationFilterDto
    {
      Page = page,
      PageSize = pageSize,
      Query = filters?.Query,
      MinPrice = filters?.Filters?.PriceRange?.Min,
      MaxPrice = filters?.Filters?.PriceRange?.Max,
      Sort = filters?.Sort,
      SteamAvailable = filters?.Filters?.Availability?.Steam,
      InstantGamingAvailable = filters?.Filters?.Availability?.InstantGaming,
      G2aAvailable = filters?.Filters?.Availability?.G2a
    };

    var filtersIncluded = filters != null;
    
    var games = filtersIncluded
      ? await _gamesWithOffersService.GetGamesPagedWithFiltersAsync(paginationFilter, paginationFilter, parsedCurrency)
      : await _gamesWithOffersService.GetGamesPagedAsync(page, pageSize);

    var totalGamesCount = filtersIncluded
      ? await _gamesRepo.GetTotalCountWithFiltersAsync(paginationFilter)
      : await _gamesRepo.GetTotalGamesCountAsync();

    _logger.LogInformation($"Total {totalGamesCount} \n First: {games.FirstOrDefault()}");
    return Ok(new {Games = games, TotalGamesCount = totalGamesCount});
  }

  [HttpGet("getPaged")]
  public async Task<IActionResult> GetPaged(int page, int pageSize){
    if (pageSize <= 0 || page < 0) return BadRequest("⚠️Invalid data provided");

    var games = await _gamesRepo.GetPagedAsync(page, pageSize);
    var totalGamesCount = await _gamesRepo.GetTotalGamesCountAsync();
    return Ok(new {Games = games, TotalGamesCount = totalGamesCount});
  }
}
