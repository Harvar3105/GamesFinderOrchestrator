using GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;
using Microsoft.AspNetCore.Authorization;
using GamesFinder.Orchestrator.API.Controllers.Contracts.InstantGaming;
using Microsoft.AspNetCore.Mvc;
using GamesFinder.Orchestrator.Domain.Interfaces.DomainServices;

namespace GamesFinder.Orchestrator.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InstantGamingController : ControllerBase
{
  private readonly ILogger<InstantGamingController> _logger;
  private readonly IInstantGamingService _instantGamingService;
  private readonly IOffersService _offersService;

  public InstantGamingController(ILogger<InstantGamingController> logger, IInstantGamingService instantGamingService, IOffersService offersService)
  {
    _logger = logger;
    _instantGamingService = instantGamingService;
    _offersService = offersService;
  }

  [HttpPost("scrapIds")]
  [Authorize(Policy = "DevPolicy")]
  public async Task<IActionResult> ScrapInstantGamingIdsAsync([FromBody] InstantGamingScrapIdsRequest model)
  {
    _logger.LogInformation("Received request to scrap Instant Gaming IDs: {Ids}", string.Join(", ", model.InstantGamingIds));
    try
    {
      await _instantGamingService.PublishIdsScrapeTaskAsync(model.InstantGamingIds, model.UpdateExisting);
      return Ok(new { Message = $"Scraping task initiated." });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error initiating scraping task for Instant Gaming IDs.");
      return StatusCode(500, "An error occurred while processing your request.");
    }
  }

  [HttpPost("scrapRange")]
  [Authorize(Policy = "DevPolicy")]
  public async Task<IActionResult> ScrapInstantGamingRangeAsync([FromBody] InstantGamingScrapRangeRequest model)
  {
    try
    {
      await _instantGamingService.PublishRangeScrapeTaskAsync(model.MinimumId, model.MaximumId, model.UpdateExisting);
      return Ok(new { Message = $"Range scraping task initiated from ID {model.MinimumId} to {model.MaximumId}." });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error initiating range scraping task for Instant Gaming IDs.");
      return StatusCode(500, "An error occurred while processing your request.");
    }
  }

  [HttpPost("scrapUpTo")]
  [Authorize(Policy = "DevPolicy")]
  public async Task<IActionResult> ScrapInstantGamingMaxCountAsync([FromBody] InstantGamingScrapUpToRequest model)
  {
    try
    {
      await _instantGamingService.PublishUpToMaxIdScrapeTaskAsync(model.MaxIdCount, model.UpdateExisting);
      return Ok(new { Message = $"Max count scraping task initiated for {model.MaxIdCount} IDs." });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error initiating max count scraping task for Instant Gaming IDs.");
      return StatusCode(500, "An error occurred while processing your request.");
    }
  }

  [HttpGet("checkGameOfferExists")]
  public async Task<IActionResult> CheckExistingInstantGamingOfferAsync(string vendorId)
  {
    try
    {
      var exists = await _offersService.CheckExistsByVendorsIdAsync(vendorId);
      return Ok(new { Exists = exists });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error checking existence of game offer for Steam ID: {vendorId}");
      return StatusCode(500, "An error occurred while processing your request.");
    }
  }

  [HttpGet("getOfferId")]
  public async Task<IActionResult> GetOfferIdByGameIdAsync(string? gameId, string? vendorId)
  {
    if ((string.IsNullOrEmpty(gameId) && vendorId == null) ||
    (!string.IsNullOrEmpty(gameId) && vendorId != null))
    {
      return BadRequest("⚠️Provide either gameId or steamId, but not both.");
    }

    try
    {
      if (!string.IsNullOrEmpty(gameId))
      {
        bool success = Guid.TryParse(gameId, out Guid parsedGameId);
        if (!success) return BadRequest("⚠️Invalid gameId format. Must be a valid GUID.");
        var id = _offersService.GetIdByGameIdAsync(parsedGameId, GamesFinder.Domain.Enums.EVendor.InstantGaming);
        return Ok(new { OfferId = id });
      }
      else
      {
        var id = await _offersService.GetIdByVendorsGameIdAsync(vendorId!, GamesFinder.Domain.Enums.EVendor.InstantGaming);
        return Ok(new { OfferId = id });
      }
    } catch (Exception ex)
    {
      _logger.LogError(ex, $"Error retrieving offer ID for gameId: {gameId} or steamId: {vendorId}");
      return StatusCode(500, "An error occurred while processing your request.");
    }
  }
}