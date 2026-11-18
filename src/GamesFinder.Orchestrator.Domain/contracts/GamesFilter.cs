using GamesFinder.Orchestrator.Domain.Enums;

namespace GamesFinder.Orchestrator.Domain.Contracts;

public sealed record GamesFilter
{
  public decimal? Min { get; init; } = null;
  public decimal? Max { get; init; } = null;
  public EVendor? Vendor { get; init; } = null;
  public ESort Sort { get; init; } = ESort.newestUpdated;
  public ICollection<string> Tags { get; init; } = [];
  public ICollection<string> Genres { get; init; } = [];
}