using GamesFinder.Orchestrator.Domain.Classes;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;
using GamesFinder.Orchestrator.Publisher;

namespace GamesFinder.Orchestrator.Services.ApplicationServices;

public class InstantGamingService : VendorsService, IInstantGamingService
{
  private readonly WorkersOptions _workersOptions;
  public InstantGamingService(InstantGamingWorkersPublisher publisher, WorkersOptions workersOptions)
    : base(publisher)
  {
    _workersOptions = workersOptions;
  }

  public async Task PublishIdsScrapeTaskAsync(List<string> vendorsIds, bool updateExisting = false)
  {
    var ids = vendorsIds.Where(id => int.Parse(id) > _workersOptions.InstantGamingSkipFirstIds);
    await BatchPublishAsync(vendorsIds, updateExisting, _workersOptions.InstantGamingWorkerCount);
  }

  public async Task PublishRangeScrapeTaskAsync(int minId, int maxId, bool updateExisting = false)
  {
    if (minId < _workersOptions.InstantGamingSkipFirstIds) minId = _workersOptions.InstantGamingSkipFirstIds;
    await BatchPublishAsync(Enumerable.Range(minId, maxId - minId + 1).Select(id => id.ToString()), updateExisting);
  }

  public async Task PublishUpToMaxIdScrapeTaskAsync(int maxId, bool updateExisting = false)
  {
    await BatchPublishAsync(Enumerable.Range(_workersOptions.InstantGamingSkipFirstIds, maxId).Select(id => id.ToString()), updateExisting);
  }
}
