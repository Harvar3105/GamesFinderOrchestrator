using System.ComponentModel.DataAnnotations;
using GamesFinder.Orchestrator.API.Controllers.Contracts.GameOffers;
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

  // 200 — the game has an offer of the vendor, 404 — it has not, 400 — invalid query. HEAD responses carry no body.
  [HttpHead("exists")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> ExistsAsync([FromQuery] GameOfferLookupRequest request)
  {
    try
    {
      var vendor = request.Vendor!.Value;
      var exists = request.GameId is { } gameId
        ? await _repo.ExistsByGameIdAsync(gameId, vendor)
        : await _repo.ExistsByVendorsIdAsync(request.VendorId!.Trim(), vendor);
      return exists ? Ok() : NotFound();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error checking existence of game offer. GameId: {request.GameId}, VendorId: {request.VendorId}, Vendor: {request.Vendor}");
      return Problem("An error occurred while processing your request.");
    }
  }

  [HttpGet("getOfferId")]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<ActionResult<GetOfferIdResponse>> GetOfferIdAsync([FromQuery] GameOfferLookupRequest request)
  {
    try
    {
      var vendor = request.Vendor!.Value;
      var id = request.GameId is { } gameId
        ? await _repo.GetIdByGameIdAsync(gameId, vendor)
        : await _repo.GetOfferIdByVendorsIdAsync(request.VendorId!.Trim(), vendor);
      if (id is null) return NotFound("Not found.");
      return Ok(new GetOfferIdResponse {Id = id.Value});
    } catch (Exception ex)
    {
      _logger.LogError(ex, $"Error retrieving offer ID. GameId: {request.GameId}, VendorId: {request.VendorId}, Vendor: {request.Vendor}");
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
  public async Task<IActionResult> GetGameOffers(string gameId, ECurrency? currency = null, bool? isAvailable = null)
  {
    if (string.IsNullOrEmpty(gameId)) return BadRequest("No gameId were provided");
    var success = Guid.TryParse(gameId, out var parsedId);
    if (!success) return BadRequest("⚠️Invalid gameId format. Must be a valid GUID.");

    try
    {
      var offers = await _repo.GetByGameIdAsync(parsedId, currency, isAvailable);
      if (offers is null) return NotFound("No offers found");
      return Ok(new { Offers = offers });
    } catch (Exception ex)
    {
      _logger.LogError(ex, $"Error retrieving offer for game: ${gameId}");
      return Problem("An error occurred while processing your request.");
    }
  }

  // Prices are compared only among offers in the requested currency; there is no conversion by design.
  // The currency is required so that a client never silently gets prices in a default currency.
  [HttpGet("getLowestPrice")]
  public async Task<IActionResult> GetLowestPrice(string gameId, [Required] ECurrency? currency)
  {
    if (string.IsNullOrEmpty(gameId)) return BadRequest("No gameId were provided");
    var success = Guid.TryParse(gameId, out var parsedId);
    if (!success) return BadRequest("⚠️Invalid gameId format. Must be a valid GUID.");

    try
    {
      var lowest = await _repo.GetMinimalOfferPrice(parsedId, currency!.Value);
      if (lowest is null) return NotFound("Could not find lowest price for this game. Try changing currency.");
      return Ok(new { Price = lowest });
    } catch (Exception ex)
    {
      _logger.LogError(ex, $"Error retrieving lowest price for game: ${gameId}");
      return Problem("An error occurred while processing your request.");
    }
  }

  [HttpGet("getAvailableVendors")]
  public async Task<IActionResult> GetAvailableVendors(string gameId)
  {
    if (string.IsNullOrEmpty(gameId)) return BadRequest("No gameId were provided");
    var success = Guid.TryParse(gameId, out var parsedId);
    if (!success) return BadRequest("⚠️Invalid gameId format. Must be a valid GUID.");

    try
    {
      var vendors = await _repo.GetAvailableVendorsByGameIdAsync(parsedId);
      return Ok(new { Vendors = vendors });
    } catch (Exception ex)
    {
      _logger.LogError(ex, $"Error retrieving available vendors for game: {gameId}");
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