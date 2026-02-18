using GamesFinder.Domain.Enums;
using GamesFinder.Orchestrator.Domain.Classes.Tasks;
using GamesFinder.Orchestrator.Domain.Interfaces.Infrastructure;

namespace GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;

public interface IVendorsService<TTask>
{
  // protected IOffersService _offersService { get; } //FIXME: Games or gameoffers ???
  protected abstract Task BatchPublishAsync(TTask task);
  protected int GetBatchSize(int totalItems, int workersCount);
  string TaskRedisKeyPrefix { get; }
}