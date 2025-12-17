using GamesFinder.Orchestrator.Publisher.RabbitMQ;
using GamesFinder.Orchestrator.Publisher.Redis;
using Microsoft.Extensions.Logging;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using Microsoft.Extensions.DependencyInjection;
using GamesFinder.Orchestrator.Domain.Interfaces.DomainServices;

namespace GamesFinder.Orchestrator.Consumers;

public class SteamWorkerConsumer : Consumer<Game>
{
  protected override string QueueName { get; }
  public SteamWorkerConsumer(
    RabbitMqConfig config,
    IServiceProvider serviceProvider,
    ILogger<SteamWorkerConsumer> logger,
    RedisCacheDB redis)
    : base(config, serviceProvider, logger, redis) { QueueName = config.SteamResultsQueue; }

  protected override async Task SaveToDatabaseAsync(IServiceScope scope, List<Game> items)
  {
    var gamesService = scope.ServiceProvider.GetRequiredService<IGamesService>();
    var success = await gamesService.SaveManyAsync(items);
    if (!success)
    {
      _logger.LogError("Could not save games!");
      return;
    }

    var offers = items.SelectMany(g => g.Offers).ToList();
    if (offers.Count() == 0)
    {
      _logger.LogInformation("No offers found for games.");
      return;
    }

    var offersService = scope.ServiceProvider.GetRequiredService<IOffersService>();
    await offersService.SaveManyAsync(offers);
  }
}
