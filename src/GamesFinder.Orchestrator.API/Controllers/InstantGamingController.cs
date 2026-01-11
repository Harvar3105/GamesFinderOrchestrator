using GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;
using Microsoft.AspNetCore.Authorization;
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
  public async Task<IActionResult> ScrapInstantGamingIdsAsync([FromBody] InstantGamingRequestModel model)
  {
    if (model.InstantGamingIds.Count == 0)
    {
      return BadRequest("Task cannot be empty.");
    }

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
  public async Task<IActionResult> ScrapInstantGamingRangeAsync([FromBody] InstantGamingRequestModel model)
  {
    if (model.MinimumId == null || model.MaximumId == null)
    {
      return BadRequest("MinimumId and MaximumId must be provided for range scraping.");
    }

    try
    {
      await _instantGamingService.PublishRangeScrapeTaskAsync(model.MinimumId.Value, model.MaximumId.Value, model.UpdateExisting);
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
  public async Task<IActionResult> ScrapInstantGamingMaxCountAsync([FromBody] InstantGamingRequestModel model)
  {
    if (model.MaxIdCount == null || model.MaxIdCount <= 10)
    {
      return BadRequest("MaxIdCount must be provided for max count scraping.");
    }

    try
    {
      await _instantGamingService.PublishUpToMaxIdScrapeTaskAsync(model.MaxIdCount.Value, model.UpdateExisting);
      return Ok(new { Message = $"Max count scraping task initiated for {model.MaxIdCount} IDs." });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error initiating max count scraping task for Instant Gaming IDs.");
      return StatusCode(500, "An error occurred while processing your request.");
    }
  }

  public sealed record InstantGamingRequestModel
  {
    public List<string> InstantGamingIds { get; init; } = new();
    public int? MaxIdCount { get; init; } = null;
    public int? MinimumId { get; init; } = null;
    public int? MaximumId { get; init; } = null;
    public bool UpdateExisting { get; init; } = false;
  }
}