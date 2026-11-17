using System.ComponentModel.DataAnnotations;

namespace GamesFinder.Orchestrator.API.Controllers.Contracts.Games;

public sealed record SearchGamesByNameRequest
{
  [Required]
  public string Name { get; init; } = string.Empty;

  [Range(1, int.MaxValue)]
  public int Page { get; init; } = 1;

  [Range(1, int.MaxValue)]
  public int PageSize { get; init; } = 20;
}
