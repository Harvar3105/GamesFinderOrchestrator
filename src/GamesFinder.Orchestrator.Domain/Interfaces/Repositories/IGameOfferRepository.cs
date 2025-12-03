using GamesFinder.Domain.Enums;
using GamesFinder.Orchestrator.Domain.Classes.Entities;

namespace GamesFinder.Domain.Interfaces.Repositories;

public interface IGameOfferRepository<TEntity> : IRepository<TEntity> where TEntity : GameOffer
{
	Task<ICollection<TEntity>?> GetByGameIdAsync(Guid gameId);
	Task<Dictionary<Guid, List<TEntity>>?> GetByGameIdsAsync(IEnumerable<Guid> gameIds);
	Task<ICollection<TEntity>?> GetByVendorAsync(EVendor vendor);
	Task<TEntity?> GetByVandorsIdAsync(string vendorsGameId);
	Task<Guid?> GetOfferIdByVandorsIdAsync(string vendorsGameId);
}