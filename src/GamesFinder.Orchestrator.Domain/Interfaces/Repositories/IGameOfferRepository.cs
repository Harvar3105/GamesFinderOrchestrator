using GamesFinder.Domain.Enums;
using GamesFinder.Orchestrator.Domain.Classes.Entities;

namespace GamesFinder.Domain.Interfaces.Repositories;

public interface IGameOfferRepository : IRepository<GameOffer>
{
	Task<ICollection<GameOffer>?> GetByGameIdAsync(Guid gameId);
	Task<Dictionary<Guid, List<GameOffer>>?> GetByGameIdsAsync(IEnumerable<Guid> gameIds);
	Task<ICollection<GameOffer>?> GetByVendorAsync(EVendor vendor);
	Task<GameOffer?> GetByVandorsIdAsync(string vendorsGameId);
	Task<Guid?> GetOfferIdByVandorsIdAsync(string vendorsGameId);
	Task<bool> ExistsByVendorsIdAsync(string vendorsId);
	Task<long> DeleteByGameIdAsync(Guid gameId);
	Task<Guid?> GetIdByGameIdAsync(Guid gameId, EVendor vendor);
	Task<Guid?> GetIdByVendorsGameIdAsync(string vendorsGameId, EVendor vendor);
}