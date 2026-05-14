using System.ComponentModel.DataAnnotations;

namespace GamesFinder.Orchestrator.API.Controllers.Contracts.InstantGaming;

public sealed record InstantGamingScrapIdsRequest
{
  [Required]
  [MinLength(1)]
  public List<string> InstantGamingIds { get; init; } = new();

  public bool UpdateExisting { get; init; } = false;
}