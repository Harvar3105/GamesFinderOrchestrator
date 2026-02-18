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
    var ids = vendorsIds.Where(id => int.Parse(id) > _workersOptions.InstantGamingSkipFirstIds);
    if (ids == null || ids.Count() == 0)
    {
      _logger.LogWarning("Steam ID list cannot be empty.");
      throw new ArgumentException("Steam ID list cannot be empty.");
    }

    var batchSize = GetBatchSize(ids.Count(), _workersOptions.InstantGamingWorkerCount);
    for (int i = 0; i < ids.Count(); i += batchSize)
    {
      var task = new InstantGamingScrapeIdsTask
      {
        GameIds = ids.Skip(i).Take(batchSize).ToList(),
        UpdateExistingDeals = updateExisting,
        RedisResultKey = TaskRedisKeyPrefix
      };
      await BatchPublishAsync(task); 
    }
  }

  public async Task PublishRangeScrapeTaskAsync(int minId, int maxId, bool updateExisting = false)
  {
    if (minId < _workersOptions.InstantGamingSkipFirstIds) throw new Exception($"Start id cannot be less than minimul id {_workersOptions.InstantGamingSkipFirstIds}");
    if (minId < 0 || maxId < 0 || maxId <= minId)
    {
      _logger.LogWarning($"Invalid range for Instant Gaming scrape task: StartId={minId}, EndId={maxId}");
      throw new ArgumentException("Invalid range for Instant Gaming scrape task.");
    }
    
    var batchSize = GetBatchSize(maxId - minId + 1, _workersOptions.InstantGamingWorkerCount);
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
    if (maxId < _workersOptions.InstantGamingSkipFirstIds) throw new Exception($"Final id cannot be less than minimul id {_workersOptions.InstantGamingSkipFirstIds}");
    if (maxId < 0)
    {
      _logger.LogWarning("Invalid upToId for Instant Gaming scrape task: UpToId={UpToId}", maxId);
      throw new ArgumentException("Invalid upToId for Instant Gaming scrape task.");
    }

    // TODO: will be assigned to 1 worker. Consider splitting int ScrapeRangeTasks
    var task = new InstantGamingScrapeUpToTask {
      UpToId = maxId,
      UpdateExistingDeals = updateExisting,
      RedisResultKey = TaskRedisKeyPrefix 
    };

    await BatchPublishAsync(task);
  }
}
