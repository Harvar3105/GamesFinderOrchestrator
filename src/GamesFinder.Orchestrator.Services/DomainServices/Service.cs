using GamesFinder.Domain.Interfaces.Repositories;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Interfaces.DomainServices;

namespace GamesFinder.Orchestrator.Services.DomainServices;

public abstract class Service<TEntity, TRepository> : IService<TEntity>
  where TEntity : Entity
  where TRepository : IRepository<TEntity>
{
  protected readonly TRepository _repository;

  public Service(TRepository repository)
  {
    _repository = repository;
  }

  public Task<long> CountAsync() => _repository.CountAsync();

  public Task<bool> DeleteAsync(Guid id) => _repository.DeleteAsync(id);

  public Task<long> DeleteManyAsync(IEnumerable<Guid> ids) => _repository.DeleteManyAsync(ids);

  public Task<bool> ExistsAsync(Guid id) => _repository.ExistsAsync(id);

  public Task<ICollection<TEntity>?> GetAllAsync() => _repository.GetAllAsync();

  public Task<TEntity?> GetByIdAsync(Guid id) => _repository.GetByIdAsync(id);

  public Task<ICollection<TEntity>?> GetPagedAsync(int page, int pageSize) => _repository.GetPagedAsync(page, pageSize);

  public Task<bool> SaveAsync(TEntity entity) => _repository.SaveAsync(entity);

  public Task<bool> SaveManyAsync(IEnumerable<TEntity> entities) => _repository.SaveManyAsync(entities);

  public Task<bool> SaveOrUpdateAsync(TEntity entity) => _repository.SaveOrUpdateAsync(entity);

  public Task<bool> SaveOrUpdateManyAsync(IEnumerable<TEntity> entities) => _repository.SaveOrUpdateManyAsync(entities);

  public Task<bool> UpdateAsync(TEntity entity) => _repository.UpdateAsync(entity);
}