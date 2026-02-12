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

  public async Task PublishIdsScrapeTaskAsync(ScrapeTask task)
  {
    if (task is not SteamScrapeTask steamTask) throw new ArgumentException("Invalid task type. Expected SteamScrapeTask.", nameof(task));

    try
    {
      await _publisher.PublishAsync(task, _config.SteamRequestsQueue);

      _logger.LogInformation($"✅Task published. ID's count: {steamTask.GameIds.Count()}, RedisKey: {steamTask.RedisResultKey}");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"💥Error publishing SteamScrapeTask (RedisKey: {task.RedisResultKey}).");
      throw;
    }
  }
}
