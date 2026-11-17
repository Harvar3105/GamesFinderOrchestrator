using GamesFinder.Orchestrator.Domain.Classes.Entities;

namespace GamesFinder.Orchestrator.API.Controllers.Contracts.Games;

public sealed record SearchGamesByNameResponse
{
  public IEnumerable<Game> Games { get; init; } = [];
  public long TotalGamesCount { get; init; }
}
