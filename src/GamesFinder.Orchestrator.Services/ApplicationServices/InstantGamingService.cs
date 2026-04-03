using GamesFinder.Orchestrator.Domain.Classes;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Classes.Tasks.InstantGaming;
using GamesFinder.Orchestrator.Domain.Interfaces.Infrastructure;
using GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;
using GamesFinder.Orchestrator.Publisher;
using Microsoft.Extensions.Logging;

namespace GamesFinder.Orchestrator.Services.ApplicationServices;

public class InstantGamingService : VendorsService<InstantGamingScrapeTask>, IInstantGamingService
{
  private readonly WorkersOptions _workersOptions;
  public InstantGamingService(PublisherFactory factory, WorkersOptions workersOptions, ILogger<InstantGamingService> logger)
    : base(factory.Create<InstantGamingScrapeTask>(), logger)
  {
    _workersOptions = workersOptions;
  }

  public override string TaskRedisKeyPrefix => $"instantgaming:scrape:result:{Guid.NewGuid()}";

  public async Task PublishIdsScrapeTaskAsync(List<string> vendorsIds, bool updateExisting = false)
  {
    if (vendorsIds == null || vendorsIds.Count() == 0)
    {
      _logger.LogWarning("Steam ID list cannot be empty.");
      throw new ArgumentException("Steam ID list cannot be empty.");
    }

    var batchSize = GetBatchSize(vendorsIds.Count());
    for (int i = 0; i < vendorsIds.Count(); i += batchSize)
    {
      var task = new InstantGamingScrapeIdsTask
      {
        GameIds = vendorsIds.Skip(i).Take(batchSize).ToList(),
        UpdateExistingDeals = updateExisting,
        RedisResultKey = TaskRedisKeyPrefix
      };
      await BatchPublishAsync(task); 
    }
  }

  public async Task PublishRangeScrapeTaskAsync(int minId, int maxId, bool updateExisting = false)
  {
    if (minId < 0 || maxId < 0 || maxId <= minId)
    {
      _logger.LogWarning($"Invalid range for Instant Gaming scrape task: StartId={minId}, EndId={maxId}");
      throw new ArgumentException("Invalid range for Instant Gaming scrape task.");
    }
    
    var batchSize = GetBatchSize(maxId - minId + 1);
    for (int i = minId; i <= maxId; i+= batchSize)
    {
      var task = new InstantGamingScrapeRangeTask
      {
        StartId = i,
        EndId = Math.Min(i + batchSize - 1, maxId),
        UpdateExistingDeals = updateExisting,
        RedisResultKey = TaskRedisKeyPrefix
      };
      await BatchPublishAsync(task);
    }
  }

  public async Task PublishUpToMaxIdScrapeTaskAsync(int maxId, bool updateExisting = false) // Max id is exclusive
  {
    if (maxId < 0)
    {
      _logger.LogWarning("Invalid upToId for Instant Gaming scrape task: UpToId={UpToId}", maxId);
      throw new ArgumentException("Invalid upToId for Instant Gaming scrape task.");
    }

    // TODO: will be assigned to 1 worker. Consider splitting int ScrapeRangeTasks
    // var task = new InstantGamingScrapeUpToTask {
    //   UpToId = maxId,
    //   UpdateExistingDeals = updateExisting,
    //   RedisResultKey = TaskRedisKeyPrefix 
    // };

    var batchSize = GetBatchSize(maxId);
    for (int i = 0; i <= maxId; i+= batchSize)
    {
      var task = new InstantGamingScrapeRangeTask
      {
        StartId = i,
        EndId = Math.Min(i + batchSize - 1, maxId),
        UpdateExistingDeals = updateExisting,
        RedisResultKey = TaskRedisKeyPrefix
      };
      await BatchPublishAsync(task);
    }
  }

  private int GetBatchSize(int totalItems)
  {
    if (_workersOptions.InstantGamingOverrideBatchSize.HasValue)
    {
      return _workersOptions.InstantGamingOverrideBatchSize.Value;
    }

    else return CalculateBatchSize(totalItems, _workersOptions.InstantGamingWorkerCount);
  }
}
