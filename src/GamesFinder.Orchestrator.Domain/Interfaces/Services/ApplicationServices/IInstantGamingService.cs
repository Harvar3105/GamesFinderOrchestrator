namespace GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;

public interface IInstantGamingService : IVendorsService
{
  Task PublishIdsScrapeTaskAsync(List<dynamic> steamIds, bool updateExisting = false);
  Task PublishRangeScrapeTaskAsync(int minId, int maxId, bool updateExisting = false);
  Task PublishUpToMaxIdScrapeTaskAsync(int maxId, bool updateExisting = false);
}