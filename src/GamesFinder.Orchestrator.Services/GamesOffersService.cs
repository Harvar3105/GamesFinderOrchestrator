using GamesFinder.Domain.Interfaces.Repositories;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Interfaces.Services;

namespace GamesFinder.Orchestrator.Services;

public class GamesOffersService : Service<GameOffer>, IOffersService
{
  public GamesOffersService(IGameOfferRepository<GameOffer> repository) : base(repository)
  {
  }

  public Task<(bool, string)> CheckIfVendorsIdExistsAsync(string vendorsId)
  {
    throw new NotImplementedException();
  }

  public Task ScrapIdsAsync(IEnumerable<int> gamesIds, bool updateExisting = false)
  {
    throw new NotImplementedException();
  }

  public Task ScrapRangeAsync(int minId, int maxId, bool updateExisting = false)
  {
    throw new NotImplementedException();
  }

  public Task ScrapUpToMaxIdAsync(int maxId, bool updateExisting = false)
  {
    throw new NotImplementedException();
  }
}