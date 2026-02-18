using GamesFinder.Orchestrator.Domain.Classes.Tasks;
using GamesFinder.Orchestrator.Domain.Interfaces.Infrastructure;
using GamesFinder.Orchestrator.Publisher.RabbitMQ;
using Microsoft.Extensions.Logging;

public class WorkerPublisher<TTask> : IPublisher<TTask> where TTask : ScrapeTask
{
  private readonly IBrockerPublisher _publisher;
  private readonly ILogger<WorkerPublisher<TTask>> _logger;
  private readonly string _queueName;

  public WorkerPublisher(IBrockerPublisher publisher, ILogger<WorkerPublisher<TTask>> logger, string queueName)
  {
    _publisher = publisher;
    _logger = logger;
    _queueName = queueName;
  }

  public async Task PublishIdsScrapeTaskAsync(TTask task)
  {
    if (task is not TTask typedTask)
      throw new ArgumentException($"Invalid task type. Expected {typeof(TTask).Name}.", nameof(task));

    try
    {
      await _publisher.PublishAsync(task, _queueName);
      _logger.LogInformation($"✅ Task published. Task: {typedTask}");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error publishing task (RedisKey: {task.RedisResultKey}).");
      throw;
    }
  }
}
