using System;
using GamesFinder.Orchestrator.Domain.Classes.Entities;

namespace GamesFinder.Orchestrator.Domain.Interfaces.Services;

public interface IInstantGamingService : IGamesWithOffersService<GameOffer>
{
  Task ScrapIdsAsync(IEnumerable<int> gamesIds, bool updateExisting = false);
  Task ScrapRangeAsync(int minId, int maxId, bool updateExisting = false);
  Task ScrapUpToMaxIdAsync(int maxId, bool updateExisting = false);
  Task<(bool, string)> CheckIfVendorsIdExistsAsync(string vendorsId);
}
