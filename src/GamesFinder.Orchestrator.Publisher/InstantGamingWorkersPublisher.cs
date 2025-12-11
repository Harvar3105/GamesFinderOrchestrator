using System;
using GamesFinder.Orchestrator.Domain.Enums;
using GamesFinder.Orchestrator.Domain.Interfaces.Infrastructure;
using GamesFinder.Orchestrator.Publisher.RabbitMQ;
using Microsoft.Extensions.Logging;

namespace GamesFinder.Orchestrator.Publisher;

public class InstantGamingWorkersPublisher : IPublisher
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

  public async Task PublishIdsScrapeTaskAsync(List<dynamic> vendorsIds, bool updateExisting = false)
  {
    if (vendorsIds == null || vendorsIds.Count == 0)
    {
      _logger.LogWarning("Instant Gaming Vendor ID list cannot be empty.");
      throw new ArgumentException("Instant Gaming task cannot be empty.");
    }

    var redisKey = $"instantgaming:scrape:result:{Guid.NewGuid()}";

    var task = new InstantGamingScrapeTask
    {
      GameIds = vendorsIds,
      UpdateExisting = updateExisting,
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

  private class InstantGamingScrapeTask
  {
    public Guid TaskId { get; set; } = Guid.NewGuid();
    public List<dynamic> GameIds { get; set; } = new();
    public bool UpdateExisting { get; set; } = false;
    public string RedisResultKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Proxy {get; set;} = null; // TODO: If found Proxy in future for workers
    public ECurrency Currency { get; set; } = ECurrency.EUR;
  }
}
