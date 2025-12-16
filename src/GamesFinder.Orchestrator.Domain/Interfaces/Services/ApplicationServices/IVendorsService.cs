using GamesFinder.Domain.Enums;
using GamesFinder.Orchestrator.Domain.Interfaces.Infrastructure;

namespace GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;

public interface IVendorsService
{
  // protected IOffersService _offersService { get; } //FIXME: Games or gameoffers ???
  protected Task BatchPublishAsync(IEnumerable<string> ids, bool updateExisting = false, int workersCount = 1);
}