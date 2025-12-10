using System;
using GamesFinder.Domain.Enums;
using GamesFinder.Orchestrator.Domain.Classes.Entities;

namespace GamesFinder.Orchestrator.Domain.Interfaces.DomainServices;

public interface IOffersService : IService<GameOffer>
{
  // Task ScrapIdsAsync(IEnumerable<int> gamesIds, bool updateExisting = false);
  // Task ScrapRangeAsync(int minId, int maxId, bool updateExisting = false);
  // Task ScrapUpToMaxIdAsync(int maxId, bool updateExisting = false);
  Task<bool> CheckExistsByVendorsIdAsync(string vendorsId);
  Task<long> DeleteByGameIdAsync(Guid gameId);
  Task<IEnumerable<GameOffer>> GetOffersByGameIdAsync(Guid gameId);
}
