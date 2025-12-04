namespace GamesFinder.Orchestrator.Domain.Interfaces.Services.VendorsServices;

public interface IVendorsService
{
  protected IOffersService _offersService { get; } //FIXME: Games or gameoffers ???
  public Task<bool> PublishTaskAsync(IEnumerable<dynamic> ids, bool updateExisting = false);
}