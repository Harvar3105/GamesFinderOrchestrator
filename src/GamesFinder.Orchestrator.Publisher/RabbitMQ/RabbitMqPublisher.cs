using GamesFinder.Orchestrator.Domain.Interfaces.Infrastructure;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace GamesFinder.Orchestrator.Publisher.RabbitMQ;

public class RabbitMqPublisher : IBrockerPublisher
{
  private readonly Lazy<Task<IConnection>> _lazyConnection;
  private readonly RabbitMqConfig _config;

  public RabbitMqPublisher(RabbitMqConfig config)
  {
    _config = config;

    _lazyConnection = new Lazy<Task<IConnection>>(async () =>
      {
        var factory = new ConnectionFactory
        {
          HostName = _config.HostName,
          Port = _config.Port,
          UserName = _config.UserName,
          Password = _config.Password
        };

        return await factory.CreateConnectionAsync();
      });
  }

  public async Task PublishAsync(object task, string? queueName = null)
  {
    var connection = await _lazyConnection.Value;
    var channel = await connection.CreateChannelAsync();

    var targetQueue = queueName ?? _config.DefaultQueue;

    await channel.QueueDeclareAsync(
      queue: targetQueue,
      durable: true,
      exclusive: false,
      autoDelete: false,
      arguments: null
    );

    var json = JsonSerializer.Serialize(task);
    var body = Encoding.UTF8.GetBytes(json);

    var props = new BasicProperties
    {
      DeliveryMode = DeliveryModes.Persistent
    };

    await channel.BasicPublishAsync(
      exchange: "",
      routingKey: targetQueue,
      mandatory: false,
      basicProperties: props,
      body: body
    );

    Console.WriteLine($"[RabbitMQ] Published to '{targetQueue}': {json}");
  }
  
  public async ValueTask DisposeAsync()
    {
      if (_lazyConnection.IsValueCreated)
      {
        var connection = await _lazyConnection.Value;
        await connection.DisposeAsync();
      }
    }
}
