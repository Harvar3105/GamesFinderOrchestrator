using GamesFinder.Domain.Enums;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Enums;

namespace GamesFinder.Domain.Interfaces.Repositories;

public interface IGameOfferRepository : IRepository<GameOffer>
{
	Task<ICollection<GameOffer>?> GetByGameIdAsync(Guid gameId);
	Task<Dictionary<Guid, List<GameOffer>>?> GetByGameIdManyAsync(IEnumerable<Guid> gameIds);
	Task<ICollection<GameOffer>?> GetByVendorAsync(EVendor vendor);
	Task<GameOffer?> GetByVendorsIdAsync(string vendorsGameId);
	Task<Guid?> GetOfferIdByVendorsIdAsync(string vendorsGameId);
	Task<bool> ExistsByVendorsIdAsync(string vendorsId);
	Task<long> DeleteByGameIdAsync(Guid gameId);
	Task<Guid?> GetIdByGameIdAsync(Guid gameId, EVendor vendor);
	Task<Guid?> GetIdByVendorsGameIdAsync(string vendorsGameId, EVendor vendor);
	Task<decimal?> GetMinimalOfferPrice(Guid gameId, ECurrency currency);
	Task<IEnumerable<long>> GetAllVendorIds(EVendor vendor);
	Task<IEnumerable<long>> GetExistingVendorIds(IEnumerable<long> vendorIdsToCheck, EVendor vendor);
}