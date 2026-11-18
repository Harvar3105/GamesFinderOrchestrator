namespace GamesFinder.Orchestrator.API.Controllers.Contracts.Games;

public sealed record GetCatalogPageResponse
{
  public IEnumerable<CatalogGame> Games { get; init; } = [];
  public long TotalGamesCount { get; init; }
}
