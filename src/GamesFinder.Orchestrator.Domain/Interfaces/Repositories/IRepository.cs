
using GamesFinder.Orchestrator.Domain.Classes.Entities;

namespace GamesFinder.Orchestrator.Domain.Interfaces.Repositories;

public interface IRepository<TEntity> where TEntity : Entity
{
	Task<bool> SaveAsync(TEntity entity);
	Task<bool> SaveManyAsync(IEnumerable<TEntity> entities);
	Task<bool> SaveOrUpdateAsync(TEntity entity);
	Task<bool> SaveOrUpdateManyAsync(IEnumerable<TEntity> entities);
	Task<bool> DeleteAsync(Guid id);
	Task<long> DeleteManyAsync(IEnumerable<Guid> ids);
	Task<bool> UpdateAsync(TEntity entity);
	Task<ICollection<TEntity>?> GetAllAsync();
	Task<TEntity?> GetByIdAsync(Guid id);
	Task<bool> ExistsAsync(Guid id);
	Task<long> CountAsync();
	Task<ICollection<TEntity>?> GetPagedAsync(int page, int pageSize);
}