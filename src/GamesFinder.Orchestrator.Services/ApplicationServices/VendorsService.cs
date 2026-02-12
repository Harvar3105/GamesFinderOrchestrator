using GamesFinder.Orchestrator.Domain.Classes.Tasks;
using GamesFinder.Orchestrator.Domain.Interfaces.Infrastructure;
using GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;
using Microsoft.Extensions.Logging;

namespace GamesFinder.Orchestrator.Services.ApplicationServices;

public abstract class VendorsService <TPublisher> : IVendorsService
  where TPublisher : class, IPublisher
{
  protected TPublisher _publisher;
  protected readonly ILogger _logger;
  public VendorsService(TPublisher publisher, ILogger logger)
  {
    _publisher = publisher;
    _logger = logger;
  }

  public abstract string TaskRedisKeyPrefix  {get;}

  public virtual async Task BatchPublishAsync(ScrapeTask task)
  {
    await _publisher.PublishIdsScrapeTaskAsync(task);
  }

  public int GetBatchSize(int totalItems, int workersCount = 1)
  {
    if (totalItems == 0) { throw new Exception("No items to process."); }
    if (workersCount <= 0) { throw new Exception("Workers count must be greater than zero."); }
    
    return (int)Math.Ceiling((double)totalItems / workersCount);
  }
}