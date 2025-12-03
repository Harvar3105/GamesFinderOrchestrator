using System;
using GamesFinder.Domain.Interfaces.Repositories;
using GamesFinder.Orchestrator.Domain.Classes;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Interfaces.Services;
using GamesFinder.Orchestrator.Publisher;

namespace GamesFinder.Orchestrator.Services;

public class InstantGamingService : GamesWithOffersService, IInstantGamingService
{
  private readonly InstantGamingWorkersPublisher _instantGamingScrapingPublisher;
  private readonly WorkersOptions _workersOptions;
  public InstantGamingService(InstantGamingWorkersPublisher instantGamingScrapingPublisher, IGameRepository<Game> gameRepository, IGameOfferRepository<GameOffer> gameOfferRepository, WorkersOptions workersOptions)
    : base(gameRepository, gameOfferRepository)
  {
    _instantGamingScrapingPublisher = instantGamingScrapingPublisher;
    _workersOptions = workersOptions;
  }

  public async Task<(bool, string)> CheckIfVendorsIdExistsAsync(string vendorsId)
  {
    var id = await _gameOfferRepository.GetOfferIdByVandorsIdAsync(vendorsId);
    if (id.HasValue) return (true, id.ToString()!);
    else return (false, string.Empty);
  }

  public async Task ScrapIdsAsync(IEnumerable<int> gamesIds, bool updateExisting = false)
  {
    gamesIds = gamesIds.Where(id => id > _workersOptions.InstantGamingSkipFirstIds);
    await BatchPublishAsync(gamesIds.ToList(), updateExisting);
  }

  public async Task ScrapRangeAsync(int minId, int maxId, bool updateExisting = false)
  {
    if (minId < _workersOptions.InstantGamingSkipFirstIds) minId = _workersOptions.InstantGamingSkipFirstIds;
    await BatchPublishAsync(Enumerable.Range(minId, maxId - minId + 1).ToList(), updateExisting);
  }

  public async Task ScrapUpToMaxIdAsync(int maxId, bool updateExisting = false)
  {
    await BatchPublishAsync(Enumerable.Range(_workersOptions.InstantGamingSkipFirstIds, maxId).ToList(),updateExisting);
  }

  private async Task BatchPublishAsync(List<int> gameIds, bool updateExisting)
  {
    var batchSize = gameIds.Count / _workersOptions.InstantGamingWorkerCount;
    for (int i = 0; i < gameIds.Count; i += batchSize)
    {
      var batch = gameIds.GetRange(i, Math.Min(batchSize, gameIds.Count - i));
      await _instantGamingScrapingPublisher.PublishInstantGamingScrapeTaskAsync(batch, updateExisting);
    }
  }
}
