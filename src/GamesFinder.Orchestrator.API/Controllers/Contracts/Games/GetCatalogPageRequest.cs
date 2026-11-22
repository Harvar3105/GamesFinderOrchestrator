using System.ComponentModel.DataAnnotations;
using GamesFinder.Orchestrator.Domain.Enums;

namespace GamesFinder.Orchestrator.API.Controllers.Contracts.Games;

public sealed record GetCatalogPageRequest
{
  // Partial name match. Omitted or blank — no name filter (the catalog start page).
  public string? Name { get; init; }

  [Range(1, int.MaxValue)]
  public int Page { get; init; } = 1;

  [Range(1, int.MaxValue)]
  public int PageSize { get; init; } = 20;

  // Prices are compared and filtered only among offers in this currency; there is no conversion by design.
  // Required (nullable so a missing value fails validation) so that prices are never silently shown in a default currency.
  [Required]
  public ECurrency? Currency { get; init; }
}
