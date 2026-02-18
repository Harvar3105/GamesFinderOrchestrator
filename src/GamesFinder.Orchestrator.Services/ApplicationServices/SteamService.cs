
using System.Text.Json.Nodes;
using GamesFinder.Domain.Interfaces.Repositories;
using GamesFinder.Orchestrator.Domain.Classes.Tasks;
using GamesFinder.Orchestrator.Domain.Interfaces.Infrastructure;
using GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;
using GamesFinder.Orchestrator.Publisher;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GamesFinder.Orchestrator.Services.ApplicationServices;

public class SteamService : VendorsService<SteamScrapeTask>, ISteamService
{
  private readonly IGameRepository _gameRepo;
  public SteamService(PublisherFactory factory, IGameRepository gameRepository, ILogger<SteamService> logger) : base(factory.Create<SteamScrapeTask>(), logger)
  {
    _gameRepo = gameRepository;
  }

  public override string TaskRedisKeyPrefix => $"steam:scrape:result:{Guid.NewGuid()}";

  public async Task PublishIdsScrapeTaskAsync(List<long> steamIds, bool updateExistingGames = false, bool updateExistingOffers = false)
  {
    if (steamIds == null || steamIds.Count() == 0)
    {
      _logger.LogWarning("Steam ID list cannot be empty.");
      throw new ArgumentException("Steam ID list cannot be empty.");
    }

    if (!updateExistingGames && !updateExistingOffers) steamIds = await RemoveExisting(steamIds);

    var batchSize = GetBatchSize(steamIds.Count(), 1);
    for (int i = 0; i < steamIds.Count(); i += batchSize)
    {
      var task = new SteamScrapeTask {
        GameIds = steamIds.Skip(i).Take(batchSize).Select(id => id.ToString()).ToList(),
        UpdateExistingDeals = updateExistingGames,
        UpdateExistingGames = updateExistingOffers,
        RedisResultKey = TaskRedisKeyPrefix
      };
      await BatchPublishAsync(task);
    }
  }

  private async Task<List<long>> RemoveExisting(List<long> steamIds)
  {
    var existingIds = await _gameRepo.ExistBySteamIdMany(steamIds);
    return existingIds.Where(r => !r.Item2).Select(r => r.Item1).ToList();
  }
}
