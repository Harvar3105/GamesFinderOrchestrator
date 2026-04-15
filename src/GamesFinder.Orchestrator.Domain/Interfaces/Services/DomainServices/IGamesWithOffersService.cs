using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Enums;

namespace GamesFinder.Orchestrator.Domain.Interfaces.DomainServices;

public interface IGamesWithOffersService
{
  Task<(bool, long)> DeleteAsync(Guid id);
  Task<(long, long)> DeleteManyAsync(IEnumerable<Guid> ids);
  Task<ICollection<Game>?> GetAllAsync();
  Task<Entity?> GetByIdAsync(Guid id);
  Task<bool> SaveAsync(Game game);
  Task<bool> SaveManyAsync(IEnumerable<Game> games);
  Task<bool> SaveOrUpdateAsync(Game game);
  Task<bool> SaveOrUpdateManyAsync(IEnumerable<Game> games);
  Task<IEnumerable<(Game, decimal?)>> GetGamesWithMinimalOffersPriceAsync(int page, int pageSize, ECurrency currency);
  Task<IEnumerable<Game>> GetGamesPagedAsync(int page, int pageSize, ECurrency? currency = null);
}