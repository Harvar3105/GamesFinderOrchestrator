using System.ComponentModel.DataAnnotations;

namespace GamesFinder.Orchestrator.API.Controllers.Contracts.Games;

public sealed record SearchGamesByNameRequest
{
  [Required]
  public string Name { get; init; } = string.Empty;

  // TODO: Adjust the upper bound after measuring response time on production-sized data.
  [Range(1, 50)]
  public int Limit { get; init; } = 10;
}
