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
  private readonly ILogger _logger;
  private readonly RedisCacheDB _redis;
  private readonly Lazy<Task<IConnection>> _lazyConnection;
  private IChannel? _channel;

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
        if (notification == null) return;

        _logger.LogInformation("Processing Redis key: {Key}", notification.RedisResultKey);

        List<TResult>? items = await GetItemsFromRedisAsync(ea.DeliveryTag, notification.RedisResultKey);
        if (items == null) return;

        if (!await TrySave(scope, items, ea.DeliveryTag)) return;

        await _redis.ClearKey(notification.RedisResultKey);

        await _channel.BasicAckAsync(ea.DeliveryTag, false);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Unhandled consumer error");
        await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
      }
    };

    await _channel.BasicConsumeAsync(QueueName, false, consumer);
    await Task.Delay(Timeout.Infinite, stoppingToken);
  }

  private async Task<bool> TrySave(IServiceScope scope, List<TResult> items, ulong deliveryTag)
  {
    try
    {
      await SaveToDatabaseAsync(scope, items);
      return true;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "DB save error");
      await _channel!.BasicNackAsync(deliveryTag, false, true);
      return false;
    }
  }

  private async Task<List<TResult>?> GetItemsFromRedisAsync(ulong deliveryTag, string redisKey)
  {
    List<TResult>? items;
    try
    {
      items = (await _redis.ListRangeAsync<TResult>(redisKey))?.ToList();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Redis error");
      await _channel!.BasicNackAsync(deliveryTag, false, true);
      return null;
    }

    if (items == null || items.Count == 0)
    {
      _logger.LogWarning("No items found in redis.");
      await _channel!.BasicRejectAsync(deliveryTag, false);
      return null;
    }
    return items;
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