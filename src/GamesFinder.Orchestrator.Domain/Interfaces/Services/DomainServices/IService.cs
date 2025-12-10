using System;
using GamesFinder.Orchestrator.Domain.Classes.Entities;

namespace GamesFinder.Orchestrator.Domain.Interfaces.DomainServices;

public interface IService<TEntity> where TEntity : Entity
{
  public Task<bool> SaveAsync(TEntity entity);
	public Task<bool> SaveManyAsync(IEnumerable<TEntity> entities);
	public Task<bool> SaveOrUpdateAsync(TEntity entity);
	public Task<bool> SaveOrUpdateManyAsync(IEnumerable<TEntity> entities);
	public Task<bool> DeleteAsync(Guid id);
  public Task<long> DeleteManyAsync(IEnumerable<Guid> ids);
	public Task<bool> UpdateAsync(TEntity entity);
	public Task<ICollection<TEntity>?> GetAllAsync();
	public Task<TEntity?> GetByIdAsync(Guid id);
	public Task<bool> ExistsAsync(Guid id);
	public Task<long> CountAsync();
	public Task<ICollection<TEntity>?> GetPagedAsync(int page, int pageSize);
}
