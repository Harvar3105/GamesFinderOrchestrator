using System;

namespace GamesFinder.Orchestrator.Domain.Interfaces.Infrastructure;

public interface IBrockerPublisher
{
  Task PublishAsync(object task, string? queueName = null);
  ValueTask DisposeAsync();
}
