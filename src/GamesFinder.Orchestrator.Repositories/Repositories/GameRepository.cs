using GamesFinder.Orchestrator.Domain.Interfaces.Repositories;
using GamesFinder.Orchestrator.Domain.Classes.DTOs;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Enums;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace GamesFinder.Orchestrator.Repositories.Repositories;

public class GameRepository : Repository<Game>, IGameRepository
{
  public GameRepository(IMongoDatabase database, ILogger<GameRepository> logger) : base(database, "games", logger)
  {
    
  }

  public async Task<Game?> GetBySteamIdAsync(long steamId)
  {
    try
    {
      return await _collection
        .Find(g => g.SteamID == steamId)
        .FirstOrDefaultAsync();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex.Message);
      return null;
    }
  }


  public async Task<bool> ExistsByAppNameAsync(string appName)
  {
    try
    {
      var allProducts = (await _collection
        .Find(_ => true)
        .Project(p => p.Name)
        .ToListAsync()).OrderBy(g => g.Length);

      return allProducts.Any(name =>
        appName.Contains(name, StringComparison.OrdinalIgnoreCase));
    }
    catch (Exception e)
    {
      _logger.LogError(e.Message);
      return false;
    }
  }

  public async Task<Game?> GetByAppNameAsync(string appName)
  {
    try
    {
      var allProducts = (await _collection
        .Find(_ => true)
        .ToListAsync())
        .OrderBy(g => g.Name.Length);

      return allProducts.FirstOrDefault(g => appName.Contains(g.Name, StringComparison.OrdinalIgnoreCase));
    }
    catch (Exception e)
    {
      _logger.LogError(e.Message);
      return null;
    }
  }

  public async Task<List<(long, bool)>> ExistBySteamIdMany(List<long> steamIds)
  {
    var filter = Builders<Game>.Filter.In(g => g.SteamID, steamIds);
    var existingIds = await _collection
        .Find(filter)
        .Project(g => g.SteamID)
        .ToListAsync();

    return steamIds
        .Select(id => (id, existingIds.Contains(id)))
        .ToList();
  }

  public async Task<bool> ExistsBySteamIdAsync(long steamId)
  {
    var filter = Builders<Game>.Filter.Eq(g => g.SteamID, steamId);
    return await _collection.Find(filter).AnyAsync();
  }

  public async Task<IEnumerable<long>> GetAllSteamIdsAsync()
  {
    return await _collection
      .Find(_ => true)
      .Project(g => g.SteamID)
      .ToListAsync();
  }
  public async Task<(bool, string)> GetExistBySteamIdAsync(long steamId)
  {
    var id = await _collection
      .Find(g => g.SteamID == steamId)
      .Project(g => g.Id)
      .FirstOrDefaultAsync();

    if (id == Guid.Empty)
      return (false, string.Empty);

    var idString = id.ToString();
    if (string.IsNullOrWhiteSpace(idString))
      return (false, string.Empty);

    return (true, idString);
  }

  public async Task<Guid?> GetIdBySteamIdAsync(long steamId)
  {
    var id = await _collection
      .Find(g => g.SteamID == steamId)
      .Project(g => g.Id)
      .FirstOrDefaultAsync();
    if (id != Guid.Empty) return id;
    else return null;
  }

  public async Task<int> GetTotalGamesCountAsync()
  {
    try
    {
      var count = await _collection.CountDocumentsAsync(_ => true);
      return (int)count;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex.Message);
      return 0;
    }
  }

  public async Task<IEnumerable<Game>> GetPagedWithFiltersAsync(PaginationFilterDto filterDto)
  {
    try
    {
      var filter = Builders<Game>.Filter.Empty;

      if (!string.IsNullOrWhiteSpace(filterDto.Query))
      {
        filter = Builders<Game>.Filter.Regex(g => g.Name, new MongoDB.Bson.BsonRegularExpression(filterDto.Query, "i"));
      }

      var skip = filterDto.Page * filterDto.PageSize;
      
      var query_builder = _collection.Find(filter).Skip(skip).Limit(filterDto.PageSize);

      if (filterDto.Sort.HasValue)
      {
        query_builder = filterDto.Sort.Value == ESort.Ascending 
          ? query_builder.SortBy(g => g.Name)
          : query_builder.SortByDescending(g => g.Name);
      }

      return await query_builder.ToListAsync();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting paged games with filters");
      return [];
    }
  }

  public async Task<int> GetTotalCountWithFiltersAsync(PaginationFilterDto filterDto)
  {
    try
    {
      var filter = Builders<Game>.Filter.Empty;

      if (!string.IsNullOrWhiteSpace(filterDto.Query))
      {
        filter = Builders<Game>.Filter.Regex(g => g.Name, new MongoDB.Bson.BsonRegularExpression(filterDto.Query, "i"));
      }

      var count = await _collection.CountDocumentsAsync(filter);
      return (int)count;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error counting games with filters");
      return 0;
    }
  }
}
