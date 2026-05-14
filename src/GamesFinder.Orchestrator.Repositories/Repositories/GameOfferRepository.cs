using System;
using GamesFinder.Orchestrator.Domain.Enums;
using GamesFinder.Orchestrator.Domain.Interfaces.Repositories;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace GamesFinder.Orchestrator.Repositories.Repositories;

public class GameOfferRepository : Repository<GameOffer>, IGameOfferRepository
{
  public GameOfferRepository(IMongoDatabase database, ILogger<Repository<GameOffer>> logger) : base(database, "game_offers", logger)
  {
  }

  public async Task<long> DeleteByGameIdAsync(Guid gameId)
  {
    return await _collection
      .DeleteManyAsync(offer => offer.GameId == gameId)
      .ContinueWith(task => task.Result.DeletedCount);
  }

  public async Task<bool> ExistsByVendorsIdAsync(string vendorsId)
  {
    return await _collection
      .Find(offer => offer.VendorsGameId.Equals(vendorsId, StringComparison.OrdinalIgnoreCase))
      .AnyAsync();
  }

  public async Task<ICollection<GameOffer>?> GetByGameIdAsync(Guid gameId)
  {
    return await _collection
      .Find(offer => offer.GameId == gameId)
      .ToListAsync();
  }

  public async Task<Dictionary<Guid, List<GameOffer>>?> GetByGameIdManyAsync(IEnumerable<Guid> gameIds)
  {
    if (gameIds == null || !gameIds.Any())
      return null;

    var idList = gameIds.ToList();

    var filter = Builders<GameOffer>.Filter.In(offer => offer.GameId, idList);

    var offers = await _collection.Find(filter).ToListAsync();

    if (!offers.Any())
      return null;

    var grouped = offers
      .GroupBy(o => o.GameId)
      .ToDictionary(g => g.Key, g => g.ToList());

    return grouped;
  }

  public async Task<GameOffer?> GetByVendorsIdAsync(string vendorsGameId)
  {
    return await _collection
      .Find(g => g.VendorsGameId.Equals(vendorsGameId, StringComparison.OrdinalIgnoreCase))
      .FirstOrDefaultAsync();
  }

  public async Task<ICollection<GameOffer>?> GetByVendorAsync(EVendor vendor)
  {
    return await _collection
      .Find(offer => offer.Vendor == vendor)
      .ToListAsync();
  }

  public async Task<Guid?> GetIdByGameIdAsync(Guid gameId, EVendor vendor)
  {
    return await _collection
      .Find(o => o.GameId.Equals(gameId) && o.Vendor == vendor)
      .Project(offer => offer.Id)
      .FirstOrDefaultAsync();
  }

  public async Task<Guid?> GetIdByVendorsGameIdAsync(string vendorsGameId, EVendor vendor)
  {
    return await _collection
      .Find(o => o.VendorsGameId.Equals(vendorsGameId, StringComparison.OrdinalIgnoreCase) && o.Vendor == vendor)
      .Project(offer => offer.Id)
      .FirstOrDefaultAsync();
  }

  public async Task<Guid?> GetOfferIdByVendorsIdAsync(string vendorsGameId)
  {
    var id = await _collection
      .Find(g => g.VendorsGameId.Equals(vendorsGameId, StringComparison.OrdinalIgnoreCase))
      .Project(offer => offer.Id)
      .FirstOrDefaultAsync();
    return id == Guid.Empty ? null : id;
  }

  public async Task<decimal?> GetMinimalOfferPrice(Guid gameId, ECurrency currency)
  {
    try
    {
      return await _collection
        .Find(offer => offer.Currency == currency && offer.GameId.Equals(gameId))
        .SortBy(offer => offer.Amount)
        .Project(offer => offer.Amount)
        .FirstOrDefaultAsync();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex.Message);
      return null;
    }
  }

  public async Task<IEnumerable<long>> GetAllVendorIds(EVendor vendor)
  {
    var filter = Builders<GameOffer>.Filter.Eq(offer => offer.Vendor, vendor);
    
    var vendorIds = await _collection
      .Find(filter)
      .Project(offer => offer.VendorsGameId)
      .ToListAsync();
    
    return vendorIds
      .Where(id => long.TryParse(id, out _))
      .Select(long.Parse);
  }

  public async Task<IEnumerable<long>> GetExistingVendorIds(IEnumerable<long> vendorIdsToCheck, EVendor vendor)
  {
    if (vendorIdsToCheck == null || !vendorIdsToCheck.Any())
      return Enumerable.Empty<long>();

    var vendorIdStrings = vendorIdsToCheck.Select(id => id.ToString()).ToList();
    var filter = Builders<GameOffer>.Filter.And(
      Builders<GameOffer>.Filter.Eq(offer => offer.Vendor, vendor),
      Builders<GameOffer>.Filter.In(offer => offer.VendorsGameId, vendorIdStrings)
    );
    
    var existingIds = await _collection
      .Find(filter)
      .Project(offer => offer.VendorsGameId)
      .ToListAsync();
    
    return existingIds
      .Select(long.Parse);
  }
}
