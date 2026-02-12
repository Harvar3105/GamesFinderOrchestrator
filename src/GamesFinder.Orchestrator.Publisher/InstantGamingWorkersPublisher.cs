using System;
using GamesFinder.Orchestrator.Domain.Classes.Tasks;
using GamesFinder.Orchestrator.Domain.Classes.Tasks.InstantGaming;
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

  public async Task PublishIdsScrapeTaskAsync(ScrapeTask task)
  {
    if (task is not InstantGamingScrapeTask igTask) throw new ArgumentException("Invalid task type. Expected InstantGamingScrapeTask.", nameof(task));

    try
    {
      await _publisher.PublishAsync(task, _config.InstantGamingRequestsQueue);

      _logger.LogInformation($"✅Task published. Task: {igTask.ToString()}");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error publishing InstantGamingScrapeTask (RedisKey: {task.RedisResultKey}).");
      throw;
    }
  }
}
