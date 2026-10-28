using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GamesFinder.Orchestrator.API.Controllers.Contracts.UserData;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamesFinder.Orchestrator.API.Controllers.Data;

// TODO: Create a service for linking the user's Steam account (Steam OpenID) and a cron job for periodic wishlist sync
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UserDataController : ControllerBase
{
  private readonly ILogger<UserDataController> _logger;
  private readonly IUserDataRepository _repo;

  public UserDataController(ILogger<UserDataController> logger, IUserDataRepository repo)
  {
    _logger = logger;
    _repo = repo;
  }

  [HttpGet("userData")]
  public async Task<IActionResult> GetUserDataAsync()
  {
    if (!TryGetUserId(out var userId)) return Unauthorized();

    try
    {
      var userData = await _repo.GetByIdAsync(userId) ?? new UserData(userId);
      return Ok(userData);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "💥 Error retrieving user data for user {UserId}", userId);
      return Problem("An error occurred while processing your request.");
    }
  }

  [HttpPut("preferences")]
  public async Task<IActionResult> UpdatePreferencesAsync([FromBody] UpdatePreferencesRequest request)
  {
    if (!TryGetUserId(out var userId)) return Unauthorized();

    var updated = await _repo.UpdatePreferencesAsync(userId, request.PreferredCurrency, request.Region);
    if (!updated) return Problem("An error occurred while processing your request.");
    return NoContent();
  }

  [HttpPost("hiddenGames")]
  public async Task<IActionResult> HideGameAsync(long steamAppId)
  {
    if (!TryGetUserId(out var userId)) return Unauthorized();

    var updated = await _repo.HideGameAsync(userId, steamAppId);
    if (!updated) return Problem("An error occurred while processing your request.");
    return NoContent();
  }

  [HttpDelete("hiddenGames")]
  public async Task<IActionResult> UnhideGameAsync(long steamAppId)
  {
    if (!TryGetUserId(out var userId)) return Unauthorized();

    var updated = await _repo.UnhideGameAsync(userId, steamAppId);
    if (!updated) return Problem("An error occurred while processing your request.");
    return NoContent();
  }

  [HttpDelete("steamAccount")]
  public async Task<IActionResult> UnlinkSteamAccountAsync()
  {
    if (!TryGetUserId(out var userId)) return Unauthorized();

    var updated = await _repo.UnlinkSteamAccountAsync(userId);
    if (!updated) return Problem("An error occurred while processing your request.");
    return NoContent();
  }

  [HttpDelete("userData")]
  public async Task<IActionResult> DeleteUserDataAsync()
  {
    if (!TryGetUserId(out var userId)) return Unauthorized();

    try
    {
      await _repo.DeleteAsync(userId);
      _logger.LogInformation("✅ User data deleted for user {UserId}", userId);
      return NoContent();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "💥 Error deleting user data for user {UserId}", userId);
      return Problem("An error occurred while processing your request.");
    }
  }

  // JwtBearer maps `sub` to ClaimTypes.NameIdentifier unless MapInboundClaims is disabled
  private bool TryGetUserId(out Guid userId)
  {
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst(JwtRegisteredClaimNames.Sub);
    return Guid.TryParse(userIdClaim?.Value, out userId);
  }
}
