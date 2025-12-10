namespace GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;

public interface ISteamService : IVendorsService
{
  Task PublishIdsScrapeTaskAsync(List<dynamic> steamIds, bool updateExisting = false);
}