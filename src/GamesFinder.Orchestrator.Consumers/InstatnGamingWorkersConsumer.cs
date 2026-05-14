using GamesFinder.Orchestrator.Publisher.RabbitMQ;
using GamesFinder.Orchestrator.Publisher.Redis;
using Microsoft.Extensions.Logging;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using Microsoft.Extensions.DependencyInjection;
using GamesFinder.Orchestrator.Domain.Interfaces.Repositories;


namespace GamesFinder.Orchestrator.Consumers;

public class InstantGamingWorkersConsumer : Consumer<GameOffer>
{
  protected override string QueueName { get; }

  public InstantGamingWorkersConsumer(
    RabbitMqConfig config,
    IServiceProvider serviceProvider,
    ILogger<InstantGamingWorkersConsumer> logger,
    RedisCacheDB redis)
    : base(config, serviceProvider, logger, redis) { QueueName = config.InstantGamingResultsQueue; }

  protected override async Task SaveToDatabaseAsync(IServiceScope scope, List<GameOffer> items)
  {
    var repo = scope.ServiceProvider.GetRequiredService<IGameOfferRepository>();
    await repo.SaveManyAsync(items);
  }
}
