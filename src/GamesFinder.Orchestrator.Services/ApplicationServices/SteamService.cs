
using GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;
using GamesFinder.Orchestrator.Publisher;

namespace GamesFinder.Orchestrator.Services.ApplicationServices;

public class SteamService : VendorsService, ISteamService
{
  public SteamService(SteamWorkerPublisher publisher) : base(publisher)
  {
  }

  public async Task PublishIdsScrapeTaskAsync(List<dynamic> steamIds, bool updateExisting = false)
  {
    await BatchPublishAsync(steamIds, updateExisting, 1);
  }
}
