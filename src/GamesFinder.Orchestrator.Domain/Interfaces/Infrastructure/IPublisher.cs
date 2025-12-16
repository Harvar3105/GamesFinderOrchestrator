namespace GamesFinder.Orchestrator.Domain.Interfaces.Infrastructure;

public interface IPublisher
{
  Task PublishIdsScrapeTaskAsync(List<string> vendorsIds, bool updateExisting = false);
}