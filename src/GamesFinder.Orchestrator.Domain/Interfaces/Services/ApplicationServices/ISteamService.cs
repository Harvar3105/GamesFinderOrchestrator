using GamesFinder.Orchestrator.Domain.Classes.Tasks;

namespace GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;

public interface ISteamService : IVendorsService<SteamScrapeTask>
{
  Task PublishIdsScrapeTaskAsync(List<long> steamIds, bool updateExisting = false, bool updateExistingOffers = false);
}