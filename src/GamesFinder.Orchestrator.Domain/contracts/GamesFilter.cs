using GamesFinder.Orchestrator.Domain.Enums;

namespace GamesFinder.Orchestrator.API.Controllers.Contracts.Steam;

// TODO: Make tags and genres as entity with strict rules
public sealed record GamesFilter
{
  public decimal? Min { get; init; } = null;
  public decimal? Max { get; init; } = null;
  public EVendor? Vendor { get; init; } = null;
  public ESort Sort { get; init; } = ESort.newestUpdated;
  public ICollection<string> Tags { get; init; } = [];
  public ICollection<string> Genres { get; init; } = [];
}