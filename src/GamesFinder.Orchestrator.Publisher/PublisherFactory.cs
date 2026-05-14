
using GamesFinder.Orchestrator.Domain.Classes.Tasks;
using GamesFinder.Orchestrator.Domain.Classes.Tasks.InstantGaming;
using GamesFinder.Orchestrator.Domain.Interfaces.Infrastructure;
using GamesFinder.Orchestrator.Publisher.RabbitMQ;
using Microsoft.Extensions.Logging;

public class PublisherFactory
{
  private readonly IBrockerPublisher _broker;
  private readonly ILoggerFactory _loggerFactory;
  private readonly RabbitMqConfig _config;

  public PublisherFactory(IBrockerPublisher broker, ILoggerFactory loggerFactory, RabbitMqConfig config)
  {
    _broker = broker;
    _loggerFactory = loggerFactory;
    _config = config;
  }

  public IPublisher<TTask> Create<TTask>() where TTask : ScrapeTask
  {
    string queueName = typeof(TTask).Name switch
    {
      nameof(SteamScrapeTask) => _config.SteamRequestsQueue,
      nameof(InstantGamingScrapeTask) => _config.InstantGamingRequestsQueue,
      _ => throw new ArgumentException("Unknown task type")
    };

    var logger = _loggerFactory.CreateLogger<WorkerPublisher<TTask>>();
    return new WorkerPublisher<TTask>(_broker, logger, queueName);
  }
}
