using GamesFinder.Domain.Interfaces.Repositories;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Interfaces.Services;

namespace GamesFinder.Orchestrator.Services;

public abstract class Service<T> : IService<T> where T : Entity
{
  protected readonly IRepository<T> _repository;

  public Service(IRepository<T> repository)
  {
    _repository = repository;
  }

  public Task<long> CountAsync() => _repository.CountAsync();

  public Task<bool> DeleteAsync(Guid id) => _repository.DeleteAsync(id);

  public Task<long> DeleteManyAsync(IEnumerable<Guid> ids) => _repository.DeleteManyAsync(ids);

  public Task<bool> ExistsAsync(Guid id) => _repository.ExistsAsync(id);

  public Task<ICollection<T>?> GetAllAsync() => _repository.GetAllAsync();

  public Task<T?> GetByIdAsync(Guid id) => _repository.GetByIdAsync(id);

  public Task<ICollection<T>?> GetPagedAsync(int page, int pageSize) => _repository.GetPagedAsync(page, pageSize);

  public Task<bool> SaveAsync(T entity) => _repository.SaveAsync(entity);

  public Task<bool> SaveManyAsync(IEnumerable<T> entities) => _repository.SaveManyAsync(entities);

  public Task<bool> SaveOrUpdateAsync(T entity) => _repository.SaveOrUpdateAsync(entity);

  public Task<bool> SaveOrUpdateManyAsync(IEnumerable<T> entities) => _repository.SaveOrUpdateManyAsync(entities);

  public Task<bool> UpdateAsync(T entity) => _repository.UpdateAsync(entity);
}