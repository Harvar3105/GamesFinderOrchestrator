using GamesFinder.Orchestrator.Domain.Interfaces.Repositories;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Enums;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
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

  // TODO: Loads the whole `games` collection into memory on every call, and the InstantGaming
  // worker calls it for each product without a Steam ID. Decide on a server-side approach:
  // a normalized-name field with an index, a MongoDB text index, or a cached name lookup.
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

  // TODO: Implement FuzzySearch so names with typos or different word order are matched too
  // (e.g. MongoDB Atlas Search with `fuzzy` options or a trigram index).
  public async Task<ICollection<Game>> SearchByNameAsync(string namePart, int page, int pageSize)
  {
    return await _collection
      .Find(NameContainsFilter(namePart))
      .SortBy(g => g.Name)
      .Skip((page - 1) * pageSize)
      .Limit(pageSize)
      .ToListAsync();
  }

  public async Task<long> CountByNameAsync(string namePart)
  {
    return await _collection.CountDocumentsAsync(NameContainsFilter(namePart));
  }

  private static FilterDefinition<Game> NameContainsFilter(string namePart)
  {
    var pattern = new BsonRegularExpression(Regex.Escape(namePart), "i");
    return Builders<Game>.Filter.Regex(g => g.Name, pattern);
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

  // Distinct over an array field returns its individual elements. Games without metadata are skipped.
  // Exceptions are not swallowed so that callers do not cache an empty list on a database error.
  public async Task<List<string>> GetDistinctTagsAsync()
  {
    var tags = await (await _collection
      .DistinctAsync<string>("store_metadata.tags", FilterDefinition<Game>.Empty))
      .ToListAsync();

    return tags
      .Where(tag => !string.IsNullOrWhiteSpace(tag))
      .Order(StringComparer.OrdinalIgnoreCase)
      .ToList();
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
}
