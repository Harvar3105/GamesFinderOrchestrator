using System.ComponentModel.DataAnnotations;
using GamesFinder.Orchestrator.Utils.Annotations;

namespace GamesFinder.Orchestrator.API.Controllers.Contracts.InstantGaming;

public sealed record InstantGamingScrapRangeRequest
{
  [Required]
  [MinimumInstantGamingId]
  public int MinimumId { get; init; }

  [Required]
  public int MaximumId { get; init; }

  public bool UpdateExisting { get; init; } = false;
}
