using GamesFinder.Orchestrator.Domain.Interfaces.Infrastructure;
using GamesFinder.Orchestrator.Publisher.RabbitMQ;
using GamesFinder.Orchestrator.Publisher.Redis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace GamesFinder.Orchestrator.Consumers;

public abstract class Consumer<TResult> : BackgroundService, IBrockerConsumer
{
  private readonly IServiceProvider _serviceProvider;
  protected readonly ILogger _logger;
  protected readonly RedisCacheDB _redis;
  private readonly Lazy<Task<IConnection>> _lazyConnection;
  protected IChannel? _channel;

  protected abstract string QueueName {get;}
  protected abstract Task SaveToDatabaseAsync(IServiceScope scope, List<TResult> items);

  protected Consumer(
    RabbitMqConfig config,
    IServiceProvider serviceProvider,
    ILogger logger,
    RedisCacheDB redis)
  {
    _serviceProvider = serviceProvider;
    _logger = logger;
    _redis = redis;

    _lazyConnection = new Lazy<Task<IConnection>>(async () =>
    {
      var factory = new ConnectionFactory
      {
        HostName = config.HostName,
        Port = config.Port,
        UserName = config.UserName,
        Password = config.Password
      };

      return await factory.CreateConnectionAsync();
    });
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    var connection = await _lazyConnection.Value;
    _channel = await connection.CreateChannelAsync();

    var consumer = new AsyncEventingBasicConsumer(_channel);
    consumer.ReceivedAsync += async (_, ea) =>
    {
      using var scope = _serviceProvider.CreateScope();

      try
      {
        var notification = await GetRedisResultNotificationAsync(ea);
        if (notification == null) throw new Exception("💥Failed to get notification from message");

        _logger.LogInformation("🔑Processing Redis key: {Key}", notification.RedisResultKey);

        List<TResult>? items = await GetItemsFromRedisAsync(ea.DeliveryTag, notification.RedisResultKey);

        await TrySave(scope, items, ea.DeliveryTag);

        await ClearRedisKeyAsync(notification.RedisResultKey);

        await _channel.BasicAckAsync(ea.DeliveryTag, false);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex.ToString());
        await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
      }
    };

    await _channel.BasicConsumeAsync(QueueName, false, consumer);
    await Task.Delay(Timeout.Infinite, stoppingToken);
  }

  private async Task TrySave(IServiceScope scope, List<TResult> items, ulong deliveryTag)
  {
    try
    {
      await SaveToDatabaseAsync(scope, items);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "DB save error");
      throw new Exception("💥Failed to save items to database!", ex);
    }
  }

  protected virtual async Task<List<TResult>> GetItemsFromRedisAsync(ulong deliveryTag, string redisKey)
  {
    List<TResult>? items;
    try
    {
      items = (await _redis.ListRangeAsync<TResult>(redisKey))?.ToList();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Redis error");
      throw new Exception("💥Failed to get items from redis!", ex);
    }

    if (items == null || items.Count == 0)
    {
      _logger.LogWarning("No items found in redis.");
      throw new Exception("⚠️No items found to process in redis!");
    }
    return items;
  }

  protected virtual async Task ClearRedisKeyAsync(string redisKey)
  {
    await _redis.ClearKey(redisKey);
  }

  private async Task<RedisResultNotification?> GetRedisResultNotificationAsync(BasicDeliverEventArgs ea)
  {
    var msg = Encoding.UTF8.GetString(ea.Body.ToArray());
    var notification = JsonSerializer.Deserialize<RedisResultNotification>(msg,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    if (notification == null)
    {
      _logger.LogWarning("Message deserialization failed");
      await _channel!.BasicRejectAsync(ea.DeliveryTag, false);
      return null;
    }
    return notification;
  }

  public async ValueTask DisposeAsync()
  {
    if (_lazyConnection.IsValueCreated)
    {
      var connection = await _lazyConnection.Value;
      await connection.DisposeAsync();
    }
  }

  private class RedisResultNotification
  {
    public string TaskId { get; set; } = string.Empty;
    public string RedisResultKey { get; set; } = string.Empty;
  }
}