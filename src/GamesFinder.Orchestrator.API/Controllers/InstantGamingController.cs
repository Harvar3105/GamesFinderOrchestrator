using GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;
using Microsoft.AspNetCore.Authorization;
using GamesFinder.Orchestrator.API.Controllers.Contracts.InstantGaming;
using Microsoft.AspNetCore.Mvc;

namespace GamesFinder.Orchestrator.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InstantGamingController : ControllerBase
{
  private readonly ILogger<InstantGamingController> _logger;
  private readonly IInstantGamingService _instantGamingService;

  public InstantGamingController(ILogger<InstantGamingController> logger, IInstantGamingService instantGamingService)
  {
    _logger = logger;
    _instantGamingService = instantGamingService;
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
}