using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Enums;

namespace GamesFinder.Orchestrator.API.Controllers.Contracts.Games;

// LowestPrice is null when the game has no offer in the requested currency (and vendor, if filtered).
// AvailableVendors are vendors with an available offer for the game, regardless of currency and vendor filter.
public sealed record CatalogGame(Game Game, decimal? LowestPrice, IEnumerable<EVendor> AvailableVendors);
