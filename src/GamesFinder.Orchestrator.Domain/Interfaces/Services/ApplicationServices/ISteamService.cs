namespace GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;

public interface ISteamService : IVendorsService
{
  Task PublishIdsScrapeTaskAsync(List<string> steamIds, bool updateExisting = false);
}