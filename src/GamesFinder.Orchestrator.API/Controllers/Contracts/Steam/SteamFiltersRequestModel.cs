using GamesFinder.Orchestrator.Domain.Enums;

namespace GamesFinder.Orchestrator.API.Controllers.Contracts.Steam;

public sealed record PriceRangeFilter
{
  public decimal? Min { get; init; } = null;
  public decimal? Max { get; init; } = null;
}

public sealed record AvailabilityFilter
{
  public bool? Steam { get; init; } = false;
  public bool? InstantGaming { get; init; } = false;
  public bool? G2a { get; init; } = false;
}

public sealed record FiltersObject
{
  public AvailabilityFilter? Availability { get; init; } = null;
  public PriceRangeFilter? PriceRange { get; init; } = null;
}

public sealed record SteamFiltersRequestModel
{
  public string? Query { get; init; } = null;
  public FiltersObject? Filters { get; init; } = null;
  public ESort? Sort { get; init; } = ESort.None;
}