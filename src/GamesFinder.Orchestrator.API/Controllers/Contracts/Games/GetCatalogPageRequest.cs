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

  public ECurrency Currency { get; init; } = ECurrency.USD;
}
