using System;
using GamesFinder.Orchestrator.Domain.Classes.Tasks.InstantGaming;
using GamesFinder.Orchestrator.Domain.Enums;
using GamesFinder.Orchestrator.Domain.Interfaces.Infrastructure;
using GamesFinder.Orchestrator.Publisher.RabbitMQ;
using Microsoft.Extensions.Logging;

namespace GamesFinder.Orchestrator.Publisher;

public class InstantGamingWorkersPublisher : IInstantGamingPublisher
{
  private readonly IBrockerPublisher _publisher;
  private readonly ILogger<SteamWorkerPublisher> _logger;
  private readonly RabbitMqConfig _config;

  public InstantGamingWorkersPublisher(IBrockerPublisher publisher, ILogger<SteamWorkerPublisher> logger, RabbitMqConfig config)
  {
    _publisher = publisher;
    _logger = logger;
    _config = config;
  }

  public async Task PublishIdsScrapeTaskAsync(List<string> vendorsIds, bool updateExisting = false)
  {
    if (vendorsIds == null || vendorsIds.Count == 0)
    {
      _logger.LogWarning("Instant Gaming Vendor ID list cannot be empty.");
      throw new ArgumentException("Instant Gaming task cannot be empty.");
    }

    var redisKey = $"instantgaming:scrape:result:{Guid.NewGuid()}";

    var task = new InstantGamingScrapeIdsTask
    {
      GameIds = vendorsIds,
      UpdateExistingDeals = updateExisting,
      RedisResultKey = redisKey
    };

    try
    {
      _logger.LogInformation($"Task publishing for Instant Gaming: {vendorsIds.Count} ID, RedisKey: {redisKey}");

      await _publisher.PublishAsync(task, _config.InstantGamingRequestsQueue);

      _logger.LogInformation($"Task published✅. ID's count: {vendorsIds.Count}");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error publishing InstantGamingScrapeTask (RedisKey: {RedisKey})", redisKey);
      throw;
    }
  }

  public async Task PublishRangeScrapeTaskAsync(int startId, int endId, bool updateExistingDeals = false)
  {
    if (startId < 0 || endId < 0 || endId <= startId)
    {
      _logger.LogWarning("Invalid range for Instant Gaming scrape task: StartId={StartId}, EndId={EndId}", startId, endId);
      throw new ArgumentException("Invalid range for Instant Gaming scrape task.");
    }

    var redisKey = $"instantgaming:scrape:result:{Guid.NewGuid()}";

    var task = new InstantGamingScrapeRangeTask
    {
      StartId = startId,
      EndId = endId,
      UpdateExistingDeals = updateExistingDeals,
      RedisResultKey = redisKey
    };

    try
    {
      _logger.LogInformation($"Task publishing for Instant Gaming: From {task.StartId} to {task.EndId}, RedisKey: {redisKey}");

      await _publisher.PublishAsync(task, _config.InstantGamingRequestsQueue);

      _logger.LogInformation($"Task published✅. ID's range: {startId} - {endId}");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error publishing InstantGamingScrapeTask (RedisKey: {RedisKey})", redisKey);
      throw;
    }
  }

  public async Task PublishUpToScrapeTaskAsync(int upToId, bool updateExistingDeals = false)
  {
    if (upToId < 0)
    {
      _logger.LogWarning("Invalid upToId for Instant Gaming scrape task: UpToId={UpToId}", upToId);
      throw new ArgumentException("Invalid upToId for Instant Gaming scrape task.");
    }

    var redisKey = $"instantgaming:scrape:result:{Guid.NewGuid()}";

    var task = new InstantGamingScrapeUpToTask
    {
      UpToId = upToId,
      UpdateExistingDeals = updateExistingDeals,
      RedisResultKey = redisKey
    };

    try
    {
      _logger.LogInformation($"Task publishing for Instant Gaming: Up to {upToId} Ids, RedisKey: {redisKey}");

      await _publisher.PublishAsync(task, _config.InstantGamingRequestsQueue);

      _logger.LogInformation($"Task published✅. Up to {upToId} Ids");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error publishing InstantGamingScrapeTask (RedisKey: {RedisKey})", redisKey);
      throw;
    }
  }
}
