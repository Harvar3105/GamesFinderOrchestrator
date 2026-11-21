namespace GamesFinder.Orchestrator.API.Controllers.Contracts.GameOffers;

public sealed record GetOfferIdResponse
{
  public Guid Id { get; init; }
}
