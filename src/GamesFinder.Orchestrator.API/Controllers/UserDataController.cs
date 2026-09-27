using System.IdentityModel.Tokens.Jwt;
using GamesFinder.Orchestrator.Domain.Classes;
using GamesFinder.Orchestrator.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamesFinder.Orchestrator.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserDataController : ControllerBase
{
  private readonly IUserDataRepository _userDataRepository;
  private readonly ILogger<UserDataController> _logger;

  public UserDataController(IUserDataRepository userDataRepository, ILogger<UserDataController> logger)
  {
    _userDataRepository = userDataRepository;
    _logger = logger;
  }

  [HttpGet("userData")]
  [Authorize]
  public async Task<IActionResult> GetUserData()
  {
    var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub);

    if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
    {
      return Unauthorized();
    }

    var userData = await _userDataRepository.GetByIdAsync(userId);
    if (userData is null) return NotFound("No user found");
    return Ok(userData);
  }

  [HttpPut("saveOrUpdate")]
  [Authorize]
  public async Task<IActionResult> SaveOrUpdate([FromBody] UserData data)
  {
    var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub);

    if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
    {
      return Unauthorized();
    }

    if (!userId.Equals(data.Id))
    {
      _logger.LogCritical($"User {userId} requested other user's update {data.Id}!");
      return Forbid("Unresolved user data!");
    }

    var result = await _userDataRepository.SaveOrUpdateAsync(data);
    if (!result) return Problem("Oops, error occured");
    return NoContent();
  }
}