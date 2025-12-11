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

  [HttpPost("scrap")]
  [Authorize(Policy = "DevPolicy")]
  public async Task<IActionResult> ScrapInstantGamingIdsAsync([FromBody] RequestModel model)
  {
    if (model.MaxIdCount == null && model.InstantGamingIds.Count == 0)
    {
      return BadRequest("Task cannot be empty.");
    }

    bool isExistingScrap = model.InstantGamingIds.Count > 0;
    try
    {
      if (isExistingScrap)
      {
        await _instantGamingService.PublishIdsScrapeTaskAsync(model.InstantGamingIds, model.UpdateExisting);
      } 
      else
      {
        if (model.MaxIdCount == null)
        {
          return BadRequest("MaxIdCount must be provided for new scraping tasks.");
        }
        await _instantGamingService.PublishUpToMaxIdScrapeTaskAsync(model.MaxIdCount.Value, model.UpdateExisting);
      }
      return Ok(new { Message = $"Scraping task initiated." });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error initiating scraping task for Instant Gaming IDs.");
      return StatusCode(500, "An error occurred while processing your request.");
    }
  }

  public sealed record RequestModel
  {
    public List<dynamic> InstantGamingIds { get; init; } = new();
    public int? MaxIdCount { get; init; } = null;
    public bool UpdateExisting { get; init; } = false;
  }
}