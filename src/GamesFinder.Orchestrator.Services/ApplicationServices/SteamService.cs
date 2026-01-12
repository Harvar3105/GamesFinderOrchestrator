
using GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;
using GamesFinder.Orchestrator.Publisher;

namespace GamesFinder.Orchestrator.Services.ApplicationServices;

public class SteamService : VendorsService<SteamWorkerPublisher>, ISteamService
{
  public SteamService(SteamWorkerPublisher publisher) : base(publisher)
  {
  }

  public async Task PublishIdsScrapeTaskAsync(List<long> steamIds, bool updateExisting = false)
  {
    await BatchPublishAsync(steamIds.Select(id => id.ToString()), updateExisting, 1);
  }
}
