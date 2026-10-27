using GamesFinder.Orchestrator.API.Controllers.Contracts.Steam;
using GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamesFinder.Orchestrator.API.Controllers.Workers;

[ApiController]
[Route("api/[controller]")]
public class SteamController : ControllerBase
{
  private readonly ILogger<SteamController> _logger;
  private readonly ISteamService _steamService;

  public SteamController(ILogger<SteamController> logger, ISteamService steamService)
  {
    _logger = logger;
    _steamService = steamService;
  }

  [HttpPost("scrap")]
  [Authorize(Roles = "admin_gfp")]
  public async Task<IActionResult> ScrapSteamIdsAsync([FromBody] SteamRequestModel model)
  {
    try
    {
      await _steamService.PublishIdsScrapeTaskAsync(model.steamIds, model.updateExistingGames, model.updateExistingOffers);
      return Created();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error initiating scraping task for Steam IDs.");
      return Problem("An error occurred while processing your request.");
    }
  }
}
