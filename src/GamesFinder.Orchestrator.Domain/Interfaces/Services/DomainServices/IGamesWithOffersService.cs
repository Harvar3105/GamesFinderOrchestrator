using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Contracts;
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
  Task<IEnumerable<(Game, decimal?)>> GetPagedWithLowestFilteredAsync(int page, int pageSize, GamesFilter filter, ECurrency currency = ECurrency.USD, string? namePart = null);
  Task<Dictionary<Guid, List<EVendor>>> GetAvailableVendorsManyAsync(IEnumerable<Guid> gameIds);
  Task<int> GetTotalCountFilteredAsync(GamesFilter filter, ECurrency currency = ECurrency.USD, string? namePart = null);
}