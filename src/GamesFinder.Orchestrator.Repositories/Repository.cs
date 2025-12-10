using GamesFinder.Domain.Interfaces.Repositories;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace GamesFinder.Orchestrator.Repositories;

public abstract class Repository<T> : IRepository<T> where T : Entity
{
  protected readonly IMongoCollection<T> _collection;
  protected readonly ILogger<Repository<T>> _logger;

  public Repository(IMongoDatabase database, string collectionName, ILogger<Repository<T>> logger)
  {
    _logger = logger;
    _collection = database.GetCollection<T>(collectionName);
  }
  
  public async Task<bool> SaveAsync(T entity)
  {
    try
    {
      await _collection.InsertOneAsync(entity);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex.Message);
      return false;
    }
    
    return true;
  }

  public async Task<bool> SaveManyAsync(IEnumerable<T> entities)
  {
    try
    {
      await _collection.InsertManyAsync(entities);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex.Message);
      return false;
    }
    
    return true;
  }

  public async Task<bool> SaveOrUpdateAsync(T entity)
  {
    try
    {
      var filter = Builders<T>.Filter.Eq(e => e.Id, entity.Id);
      await _collection.ReplaceOneAsync(filter, entity, new ReplaceOptions { IsUpsert = true });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex.Message);
      return false;
    }
    
    return true;
  }

  public async Task<bool> SaveOrUpdateManyAsync(IEnumerable<T> entities)
  {
    try
    {
      var models = new List<WriteModel<T>>();

      foreach (var entity in entities)
      {
        var filter = Builders<T>.Filter.Eq(e => e.Id, entity.Id);
        var replaceOne = new ReplaceOneModel<T>(filter, entity) {IsUpsert = true};
        models.Add(replaceOne);
      }

      var result = await _collection.BulkWriteAsync(models);
      return result != null;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex.Message);
      return false;
    }
  }

  public async Task<bool> DeleteAsync(Guid id)
  {
    var result = await _collection.DeleteOneAsync(e => e.Id == id);
    return result.DeletedCount > 0;
  }

  public async Task<bool> UpdateAsync(T entity)
  {
    var result = await _collection.ReplaceOneAsync(e => e.Id == entity.Id, entity);
    return result.ModifiedCount > 0;
  }

  public async Task<ICollection<T>?> GetAllAsync()
  {
    return await _collection.Find(_ => true).ToListAsync();
  }

  public async Task<T?> GetByIdAsync(Guid id)
  {
    return await _collection.Find(e => e.Id == id).FirstOrDefaultAsync();
  }

  public async Task<bool> ExistsAsync(Guid id)
  {
    return await _collection.Find(e => e.Id == id).AnyAsync();
  }

  public async Task<long> CountAsync()
  {
    return await _collection.CountDocumentsAsync(_ => true);
  }
  
  public async Task<ICollection<T>?> GetPagedAsync(int page, int pageSize)
  {
    return await _collection
      .Find(_ => true)
      .Skip((page - 1) * pageSize)
      .Limit(pageSize)
      .ToListAsync();
  }

  public async Task<long> DeleteManyAsync(IEnumerable<Guid> ids)
  {
    var result = await _collection.DeleteManyAsync(e => ids.Contains(e.Id));
    return result.DeletedCount;
  } 
}