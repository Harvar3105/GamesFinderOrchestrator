namespace GamesFinder.Orchestrator.API.Controllers.Contracts.Games;

public sealed record GetGameIdsResponse
{
  public Guid Id { get; init; }

  public long SteamId { get; init; }
}
