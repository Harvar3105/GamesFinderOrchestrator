using GamesFinder.Orchestrator.Domain.Interfaces.Infrastructure;
using GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;

namespace GamesFinder.Orchestrator.Services.ApplicationServices;

public abstract class VendorsService <TPublisher> : IVendorsService
  where TPublisher : class, IPublisher
{
  protected TPublisher _publisher;
  public VendorsService(TPublisher publisher)
  {
    _publisher = publisher;
  }

  public async Task BatchPublishAsync(IEnumerable<string> ids, bool updateExisting = false, int workersCount = 1)
  {
    if (ids.Count() == 0)
    {
      throw new Exception("No IDs to process after applying filters.");
    }

    if (ids.Count() < workersCount)
    {
      workersCount = ids.Count();
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