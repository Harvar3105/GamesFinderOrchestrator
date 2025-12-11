using GamesFinder.Orchestrator.Publisher.RabbitMQ;
using GamesFinder.Orchestrator.Publisher.Redis;
using Microsoft.Extensions.Logging;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using Microsoft.Extensions.DependencyInjection;
using GamesFinder.Orchestrator.Domain.Interfaces.DomainServices;


namespace GamesFinder.Orchestrator.Consumers;

public class InstantGamingWorkersConsumer : Consumer<GameOffer>
{
  protected override string QueueName => "instant-gaming-results";

  public InstantGamingWorkersConsumer(
    RabbitMqConfig config,
    IServiceProvider serviceProvider,
    ILogger<InstantGamingWorkersConsumer> logger,
    RedisCacheDB redis)
    : base(config, serviceProvider, logger, redis) { }

  protected override async Task SaveToDatabaseAsync(IServiceScope scope, List<GameOffer> items)
  {
    var service = scope.ServiceProvider.GetRequiredService<IOffersService>();
    await service.SaveManyAsync(items);
  }
}
