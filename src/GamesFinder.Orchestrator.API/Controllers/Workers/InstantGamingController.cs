using GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;
using Microsoft.AspNetCore.Authorization;
using GamesFinder.Orchestrator.API.Controllers.Contracts.InstantGaming;
using Microsoft.AspNetCore.Mvc;

namespace GamesFinder.Orchestrator.API.Controllers.Workers;

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
  [Authorize(Roles = "admin_gfp")]
  public async Task<IActionResult> ScrapInstantGamingIdsAsync([FromBody] InstantGamingScrapIdsRequest model)
  {
    _logger.LogInformation("Received request to scrap Instant Gaming IDs: {Ids}", string.Join(", ", model.InstantGamingIds));
    try
    {
      await _instantGamingService.PublishIdsScrapeTaskAsync(model.InstantGamingIds, model.UpdateExisting);
      return Created();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error initiating scraping task for Instant Gaming IDs.");
      return Problem("An error occurred while processing your request.");
    }
  }

  [HttpPost("scrapRange")]
  [Authorize(Roles = "admin_gfp")]
  public async Task<IActionResult> ScrapInstantGamingRangeAsync([FromBody] InstantGamingScrapRangeRequest model)
  {
    try
    {
      await _instantGamingService.PublishRangeScrapeTaskAsync(model.MinimumId, model.MaximumId, model.UpdateExisting);
      return Created();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error initiating range scraping task for Instant Gaming IDs.");
      return Problem("An error occurred while processing your request.");
    }
  }

  [HttpPost("scrapUpTo")]
  [Authorize(Roles = "admin_gfp")]
  public async Task<IActionResult> ScrapInstantGamingUpToAsync([FromBody] InstantGamingScrapUpToRequest model)
  {
    try
    {
      await _instantGamingService.PublishUpToMaxIdScrapeTaskAsync(model.MaxIdCount, model.UpdateExisting);
      return Created();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error initiating max count scraping task for Instant Gaming IDs.");
      return Problem("An error occurred while processing your request.");
    }
  }
}