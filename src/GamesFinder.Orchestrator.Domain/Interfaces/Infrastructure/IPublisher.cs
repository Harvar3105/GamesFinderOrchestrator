namespace GamesFinder.Orchestrator.Domain.Interfaces.Infrastructure;

public interface IPublisher
{
  Task PublishIdsScrapeTaskAsync(List<dynamic> vendorsIds, bool updateExisting = false);
}