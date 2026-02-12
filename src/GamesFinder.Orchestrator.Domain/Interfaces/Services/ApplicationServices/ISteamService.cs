namespace GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;

public interface ISteamService : IVendorsService
{
  Task PublishIdsScrapeTaskAsync(List<long> steamIds, bool updateExisting = false, bool updateExistingOffers = false);
}