using System.ComponentModel.DataAnnotations;
using GamesFinder.Orchestrator.Utils.Annotations;

namespace GamesFinder.Orchestrator.API.Controllers.Contracts.InstantGaming;

public sealed record InstantGamingScrapUpToRequest
{
  [Required]
  [MinimumInstantGamingId]
  public int MaxIdCount { get; init; }

  public bool UpdateExisting { get; init; } = false;
}
