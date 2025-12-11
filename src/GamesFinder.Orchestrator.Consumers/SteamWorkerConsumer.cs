using GamesFinder.Orchestrator.Publisher.RabbitMQ;
using GamesFinder.Orchestrator.Publisher.Redis;
using Microsoft.Extensions.Logging;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using Microsoft.Extensions.DependencyInjection;
using GamesFinder.Orchestrator.Domain.Interfaces.DomainServices;

namespace GamesFinder.Orchestrator.Consumers;

public class SteamWorkerConsumer : Consumer<Game>
{
  protected override string QueueName => "steam-scraper-results";

  public SteamWorkerConsumer(
    RabbitMqConfig config,
    IServiceProvider serviceProvider,
    ILogger<SteamWorkerConsumer> logger,
    RedisCacheDB redis)
    : base(config, serviceProvider, logger, redis) { }

  protected override async Task SaveToDatabaseAsync(IServiceScope scope, List<Game> items)
  {
    var service = scope.ServiceProvider.GetRequiredService<IGamesService>();
    await service.SaveManyAsync(items);
  }
}
