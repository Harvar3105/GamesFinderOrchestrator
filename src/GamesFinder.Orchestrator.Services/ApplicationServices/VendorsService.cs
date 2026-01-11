using GamesFinder.Orchestrator.Domain.Interfaces.Infrastructure;
using GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;

namespace GamesFinder.Orchestrator.Services.ApplicationServices;

public abstract class VendorsService : IVendorsService
{
  protected IPublisher _publisher;
  public VendorsService(IPublisher publisher)
  {
    _publisher = publisher;
  }

  public async Task BatchPublishAsync(IEnumerable<string> ids, bool updateExisting = false, int workersCount = 1)
  {
    if (ids.Count() == 0)
    {
      await _publisher.PublishIdsScrapeTaskAsync(ids.ToList(), updateExisting);
      return;
    }

    var asList = ids.ToList();
    var batchSize = asList.Count / workersCount;
    for (int i = 0; i < asList.Count; i += batchSize)
    {
      var batch = asList.GetRange(i, Math.Min(batchSize, asList.Count - i));
      await _publisher.PublishIdsScrapeTaskAsync(batch, updateExisting);
    }
  }
}