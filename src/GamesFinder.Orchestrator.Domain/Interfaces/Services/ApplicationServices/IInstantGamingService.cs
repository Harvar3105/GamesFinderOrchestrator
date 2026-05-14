using GamesFinder.Orchestrator.Domain.Classes.Tasks.InstantGaming;

namespace GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;

public interface IInstantGamingService : IVendorsService<InstantGamingScrapeTask>
{
  Task PublishIdsScrapeTaskAsync(List<string> steamIds, bool updateExisting = false);
  Task PublishRangeScrapeTaskAsync(int minId, int maxId, bool updateExisting = false);
  Task PublishUpToMaxIdScrapeTaskAsync(int maxId, bool updateExisting = false);
}