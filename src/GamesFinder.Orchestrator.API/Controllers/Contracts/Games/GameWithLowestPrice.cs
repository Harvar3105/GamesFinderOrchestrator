using GamesFinder.Orchestrator.Domain.Classes.Entities;

namespace GamesFinder.Orchestrator.API.Controllers.Contracts.Games;

// LowestPrice is null when the game has no offer in the requested currency (and vendor, if filtered).
public sealed record GameWithLowestPrice(Game Game, decimal? LowestPrice);
