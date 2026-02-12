using GamesFinder.Domain.Enums;
using GamesFinder.Domain.Interfaces.Repositories;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Interfaces.DomainServices;

namespace GamesFinder.Orchestrator.Services.DomainServices;

public class GamesOffersService : Service<GameOffer, IGameOfferRepository>, IOffersService
{
  public GamesOffersService(IGameOfferRepository repository) : base(repository)
  {
    
  }

  public Task<bool> CheckExistsByVendorsIdAsync(string vendorsId)
  {
    return _repository.ExistsByVendorsIdAsync(vendorsId);
  }

  public Task<long> DeleteByGameIdAsync(Guid gameId)
  {
    return _repository.DeleteByGameIdAsync(gameId);
  }

  public Task<Guid?> GetIdByGameIdAsync(Guid gameId, EVendor vendor)
  {
    return _repository.GetIdByGameIdAsync(gameId, vendor);
  }

  public Task<Guid?> GetIdByVendorsGameIdAsync(string vendorsGameId, EVendor vendor)
  {
    return _repository.GetIdByVendorsGameIdAsync(vendorsGameId, vendor);
  }

  public async Task<IEnumerable<GameOffer>> GetOffersByGameIdAsync(Guid gameId)
  {
    return await _repository.GetByGameIdAsync(gameId) ?? new List<GameOffer>();
  }
}