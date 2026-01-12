namespace GamesFinder.Orchestrator.Domain.Interfaces.Infrastructure;

public interface IInstantGamingPublisher : IPublisher
{
  Task PublishRangeScrapeTaskAsync(int startId, int endId, bool updateExistingDeals = false);
  Task PublishUpToScrapeTaskAsync(int upToId,  bool updateExistingDeals = false);
}