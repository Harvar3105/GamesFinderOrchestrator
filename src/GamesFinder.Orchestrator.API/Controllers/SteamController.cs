using GamesFinder.Orchestrator.API.Controllers.Contracts.Steam;
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
  private readonly IGamesService _gamesService;
  private readonly IOffersService _offersService;

  public SteamController(ILogger<SteamController> logger, ISteamService steamService, IGamesService gamesService, IOffersService offersService)
  {
    _logger = logger;
    _steamService = steamService;
    _gamesService = gamesService;
    _offersService = offersService;
  }

  [HttpPost("scrap")]
  [Authorize(Policy = "DevPolicy")]
  public async Task<IActionResult> ScrapSteamIdsAsync([FromBody] SteamRequestModel model)
  {
    try
    {
      await _steamService.PublishIdsScrapeTaskAsync(model.steamIds, model.updateExisting);
      return Ok(new { Message = $"Scraping task initiated for {model.steamIds.Count} Steam IDs. Estimated time: {MathF.Ceiling(model.steamIds.Count / 200)} minutes" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error initiating scraping task for Steam IDs.");
      return StatusCode(500, "An error occurred while processing your request.");
    }
  }

  [HttpPost("checkGameExists")]
  public async Task<IActionResult> CheckExistingSteamIdAsync(int steamId, bool getGame = false)
  {
    try
    {
      var exists = await _gamesService.CheckIfSteamIdExistsAsync(steamId);
      if (getGame && exists)
      {
        var game = await _gamesService.GetBySteamIdAsync(steamId);
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

  [HttpPost("getGameIdBySteamId")]
  public async Task<IActionResult> GetGameIdBySteamIdAsync(int steamId)
  {
    try
    {
      bool exists = await _gamesService.CheckIfSteamIdExistsAsync(steamId);
      if (exists) {
        var gameId = await _gamesService.GetIdBySteamIdAsync(steamId);
        return Ok(new {id = gameId});
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

  [HttpPost("checkGameOfferExists")]
  public async Task<IActionResult> CheckExistingGameOfferAsync(int steamId)
  {
    try
    {
      var exists = await _offersService.CheckExistsByVendorsIdAsync(steamId.ToString());
      return Ok(new { Exists = exists });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error checking existence of game offer for Steam ID: {steamId}");
      return StatusCode(500, "An error occurred while processing your request.");
    }
  }
}
