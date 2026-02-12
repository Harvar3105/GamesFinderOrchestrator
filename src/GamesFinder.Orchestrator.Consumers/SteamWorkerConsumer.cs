using GamesFinder.Orchestrator.Publisher.RabbitMQ;
using GamesFinder.Orchestrator.Publisher.Redis;
using Microsoft.Extensions.Logging;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using Microsoft.Extensions.DependencyInjection;
using GamesFinder.Orchestrator.Domain.Interfaces.DomainServices;

namespace GamesFinder.Orchestrator.Consumers;

public class SteamWorkerConsumer : Consumer<GameOrOffer>
{
  protected override string QueueName { get; }
  public SteamWorkerConsumer(
    RabbitMqConfig config,
    IServiceProvider serviceProvider,
    ILogger<SteamWorkerConsumer> logger,
    RedisCacheDB redis)
    : base(config, serviceProvider, logger, redis) { QueueName = config.SteamResultsQueue; }

  protected override async Task SaveToDatabaseAsync(IServiceScope scope, List<GameOrOffer> items)
  {
    var gamesService = scope.ServiceProvider.GetRequiredService<IGamesService>();
    var offersService = scope.ServiceProvider.GetRequiredService<IOffersService>();

    var games = items.Where(i => i.IsGame).Select(i => i.Game!).ToList();
    var offers = items.Where(i => !i.IsGame).Select(i => i.Offer!).ToList();
    if (games != null && games.Count() > 0) offers.AddRange(games.Where(g => g.Offers != null).Select(g => g.Offers!.First()));

    if (games != null && games.Count() > 0)
    {
      var success = await gamesService.SaveManyAsync(games);
      if (!success) _logger.LogError("💥Could not save games!");
    }
    
    if (offers != null && offers.Count() > 0)
    {
      var success = await offersService.SaveManyAsync(offers);
      if (!success) _logger.LogError("💥Could not save offers!");
    }
  }

  protected override async Task<List<GameOrOffer>?> GetItemsFromRedisAsync(ulong deliveryTag, string redisKey)
  {
    List<GameOrOffer>? items = [];
    try
    {
      var games = await _redis.ListRangeAsync<Game>($"{redisKey}:games");
      var offers = await _redis.ListRangeAsync<GameOffer>($"{redisKey}:offers");

      var incapsulatedGames = games?.Select(g => new GameOrOffer(game: g, offer: null));
      var incapsulatedOffers = offers?.Select(o => new GameOrOffer(game: null, offer: o)); 
      
      if (incapsulatedGames != null) items.AddRange(incapsulatedGames);
      if (incapsulatedOffers != null) items.AddRange(incapsulatedOffers);
      return items;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Redis error");
      await _channel!.BasicNackAsync(deliveryTag, false, true);
      return null;
    }
  }

  protected override async Task ClearRedisKeyAsync(string redisKey)
  {
    await _redis.ClearKey(redisKey);
    await _redis.ClearKey($"{redisKey}:games");
    await _redis.ClearKey($"{redisKey}:offers");
  }
}

public record GameOrOffer
{
  public Game? Game { get; set; }
  public GameOffer? Offer { get; set; }
  public bool IsGame => Game != null;

  public GameOrOffer(Game? game, GameOffer? offer)
  {
    Game = game;
    Offer = offer;
  }
}
