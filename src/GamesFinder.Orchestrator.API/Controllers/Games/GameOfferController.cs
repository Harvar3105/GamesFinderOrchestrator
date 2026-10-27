using GamesFinder.Orchestrator.Domain.Enums;
using GamesFinder.Orchestrator.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace GamesFinder.Orchestrator.API.Controllers.Games;

[ApiController]
[Route("api/[controller]")]
public class GameOfferController : ControllerBase
{
  private readonly ILogger<GameOfferController> _logger;
  private readonly IGameOfferRepository _repo;

  public GameOfferController(ILogger<GameOfferController> logger, IGameOfferRepository repo)
  {
    this._logger = logger;
    this._repo = repo;
  }

  [HttpGet("checkGameOfferExists")]
  public async Task<IActionResult> CheckExistingGameOfferAsync(int steamId, EVendor vendor)
  {
    try
    {
      var exists = await _repo.ExistsByVendorsIdAsync(steamId.ToString(), vendor);
      return Ok(new { Exists = exists });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error checking existence of game offer for Steam ID: {steamId}");
      return Problem("An error occurred while processing your request.");
    }
  }

  [HttpGet("getOfferIdByGameId")]
  public async Task<IActionResult> GetOfferIdByGameIdAsync(string? gameId, EVendor vendor)
  {
    if (string.IsNullOrEmpty(gameId))
    {
      return BadRequest("⚠️Provide gameId and VendorId");
    }

    var success = Guid.TryParse(gameId, out Guid parsedId);
    if (!success) return BadRequest("⚠️Invalid gameId format. Must be a valid GUID.");

    try
    {
      var id = await _repo.GetIdByGameIdAsync(parsedId, vendor);
      if (id is null) return NotFound("Not found.");
      return Ok(new { Id = id });
    } catch (Exception ex)
    {
      _logger.LogError(ex, $"Error retrieving offer ID for gameId: {gameId} of vendor: {vendor}");
      return Problem("An error occurred while processing your request.");
    }
  }

  [HttpGet("getOfferIdByVendorId")]
  public async Task<IActionResult> GetOfferIdByVendorId(string vendorId, EVendor vendor)
  {
    if (string.IsNullOrEmpty(vendorId))
    {
      return BadRequest("⚠️Provide gameId and VendorId");
    }

    try
    {
      var id = await _repo.GetIdByVendorsGameIdAsync(vendorId, EVendor.InstantGaming);
      if (id is null) return NotFound("Not found.");
      return Ok(new { Id = id });
    } catch (Exception ex)
    {
      _logger.LogError(ex, $"Error retrieving offer ID for vendorIf: {vendorId} of: {vendor}");
      return Problem("An error occurred while processing your request.");
    }
  }

  [HttpGet("getGameOffer")]
  public async Task<IActionResult> GetGameOffer(string id)
  {
    if (string.IsNullOrEmpty(id)) return BadRequest("No id were provided");
    var success = Guid.TryParse(id, out var parsedId);
    if (!success) return BadRequest("⚠️Invalid gameId format. Must be a valid GUID.");

    try
    {
      var offer = await _repo.GetByIdAsync(parsedId);
      if (offer is null) return NotFound("No offer with such id.");
      return Ok(new { Offer = offer });
    } catch (Exception ex)
    {
      _logger.LogError(ex, $"Error retrieving offer with ID: ${id}");
      return Problem("An error occurred while processing your request.");
    }
  }

  [HttpGet("getGameOffers")]
  public async Task<IActionResult> GetGameOffers(string gameId)
  {
    if (string.IsNullOrEmpty(gameId)) return BadRequest("No gameId were provided");
    var success = Guid.TryParse(gameId, out var parsedId);
    if (!success) return BadRequest("⚠️Invalid gameId format. Must be a valid GUID.");

    try
    {
      var offers = await _repo.GetByGameIdAsync(parsedId);
      if (offers is null) return NotFound("No offers found");
      return Ok(new { Offers = offers });
    } catch (Exception ex)
    {
      _logger.LogError(ex, $"Error retrieving offer for game: ${gameId}");
      return Problem("An error occurred while processing your request.");
    }
  }

  [HttpGet("getLowestPrice")]
  public async Task<IActionResult> GetLowestPrice(string gameId, ECurrency currency = ECurrency.USD)
  {
    if (string.IsNullOrEmpty(gameId)) return BadRequest("No gameId were provided");
    var success = Guid.TryParse(gameId, out var parsedId);
    if (!success) return BadRequest("⚠️Invalid gameId format. Must be a valid GUID.");

    try
    {
      var lowest = await _repo.GetMinimalOfferPrice(parsedId, currency);
      if (lowest is null) return NotFound("Could not find lowest price for this game. Try changing currency.");
      return Ok(new { Price = lowest });
    } catch (Exception ex)
    {
      _logger.LogError(ex, $"Error retrieving lowest price for game: ${gameId}");
      return Problem("An error occurred while processing your request.");
    }
  }

  [HttpGet("vendors")]
  public async Task<IActionResult> GetVendors()
  {
    // TODO: Vendors are hardcoded, should be entities.
    List<EVendor> list = [EVendor.Steam, EVendor.InstantGaming, EVendor.G2A];
    return Ok(new { Vendors = list});
  }
}