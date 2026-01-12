using GamesFinder.Orchestrator.Domain.Classes.Tasks;
using GamesFinder.Orchestrator.Domain.Interfaces.Infrastructure;
using GamesFinder.Orchestrator.Publisher.RabbitMQ;
using Microsoft.Extensions.Logging;

namespace GamesFinder.Orchestrator.Publisher;

public class SteamWorkerPublisher : IPublisher
{
  private readonly IBrockerPublisher _publisher;
  private readonly ILogger<SteamWorkerPublisher> _logger;
  private readonly RabbitMqConfig _config;

  public SteamWorkerPublisher(IBrockerPublisher publisher, ILogger<SteamWorkerPublisher> logger, RabbitMqConfig config)
  {
    _publisher = publisher;
    _logger = logger;
    _config = config;
  }

  public async Task PublishIdsScrapeTaskAsync(List<string> steamIds, bool updateExisting = false)
  {
    if (steamIds == null || steamIds.Count == 0)
    {
      _logger.LogWarning("Steam ID list cannot be empty.");
      throw new ArgumentException("Steam ID list cannot be empty.");
    }

    var redisKey = $"steam:scrape:result:{Guid.NewGuid()}";

    var task = new SteamScrapeTask
    {
      GameIds = steamIds,
      UpdateExistingDeals = updateExisting,
      UpdateExistingGames = updateExisting,
      RedisResultKey = redisKey
    };

    try
    {
      _logger.LogInformation("Task publishing for Steam: {Count} ID, RedisKey: {RedisKey}", steamIds.Count, redisKey);

      await _publisher.PublishAsync(task, _config.SteamRequestsQueue);

      _logger.LogInformation("Task published✅. ID's count: {Count}", steamIds.Count);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error publishing SteamScrapeTask (RedisKey: {RedisKey})", redisKey);
      throw;
    }
  }
}
