using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Enums;
using GamesFinder.Orchestrator.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace GamesFinder.Orchestrator.Repositories.Repositories;

public class UserDataRepository : Repository<UserData>, IUserDataRepository
{
  public UserDataRepository(IMongoDatabase database, ILogger<Repository<UserData>> logger) : base(database, "users_data", logger)
  {
  }

  public Task<bool> LinkSteamAccountAsync(Guid userId, long steamId64)
  {
    var update = Builders<UserData>.Update
      .Set(u => u.SteamId64, steamId64)
      .Set(u => u.SteamWishlist, [])
      .Set(u => u.WishlistSyncedAt, null)
      .Set(u => u.WishlistSyncStatus, EWishlistSyncStatus.NotSynced);

    return UpsertAsync(userId, update);
  }

  public Task<bool> UnlinkSteamAccountAsync(Guid userId)
  {
    var update = Builders<UserData>.Update
      .Set(u => u.SteamId64, null)
      .Set(u => u.SteamWishlist, [])
      .Set(u => u.WishlistSyncedAt, null)
      .Set(u => u.WishlistSyncStatus, EWishlistSyncStatus.NotSynced);

    return UpsertAsync(userId, update);
  }

  public Task<bool> ReplaceWishlistAsync(Guid userId, IEnumerable<WishlistItem> wishlist, EWishlistSyncStatus status)
  {
    var update = Builders<UserData>.Update
      .Set(u => u.SteamWishlist, wishlist.ToList())
      .Set(u => u.WishlistSyncedAt, DateTime.UtcNow)
      .Set(u => u.WishlistSyncStatus, status);

    return UpsertAsync(userId, update);
  }

  public Task<bool> SetWishlistSyncStatusAsync(Guid userId, EWishlistSyncStatus status)
  {
    var update = Builders<UserData>.Update
      .Set(u => u.WishlistSyncedAt, DateTime.UtcNow)
      .Set(u => u.WishlistSyncStatus, status);

    return UpsertAsync(userId, update);
  }

  public Task<bool> HideGameAsync(Guid userId, long steamAppId)
  {
    return UpsertAsync(userId, Builders<UserData>.Update.AddToSet(u => u.HiddenSteamIds, steamAppId));
  }

  public Task<bool> UnhideGameAsync(Guid userId, long steamAppId)
  {
    return UpsertAsync(userId, Builders<UserData>.Update.Pull(u => u.HiddenSteamIds, steamAppId));
  }

  public Task<bool> UpdatePreferencesAsync(Guid userId, ECurrency preferredCurrency, string? region)
  {
    var update = Builders<UserData>.Update
      .Set(u => u.PreferredCurrency, preferredCurrency)
      .Set(u => u.Region, region?.ToUpperInvariant());

    return UpsertAsync(userId, update);
  }

  public async Task<ICollection<UserData>> GetStaleWishlistsAsync(DateTime syncedBefore, int limit)
  {
    var filter = Builders<UserData>.Filter.And(
      Builders<UserData>.Filter.Ne(u => u.SteamId64, null),
      Builders<UserData>.Filter.Or(
        Builders<UserData>.Filter.Eq(u => u.WishlistSyncedAt, null),
        Builders<UserData>.Filter.Lt(u => u.WishlistSyncedAt, syncedBefore)
      )
    );

    return await _collection
      .Find(filter)
      .SortBy(u => u.WishlistSyncedAt)
      .Limit(limit)
      .ToListAsync();
  }

  public async Task<ICollection<long>> GetDistinctWishlistSteamAppIdsAsync()
  {
    var cursor = await _collection.DistinctAsync<long>("steam_wishlist.steam_app_id", Builders<UserData>.Filter.Empty);
    return await cursor.ToListAsync();
  }

  private async Task<bool> UpsertAsync(Guid userId, UpdateDefinition<UserData> update)
  {
    try
    {
      var now = DateTime.UtcNow;
      update = update
        .Set(u => u.UpdatedAt, now)
        .SetOnInsert(u => u.CreatedAt, now);

      var result = await _collection.UpdateOneAsync(
        u => u.Id == userId,
        update,
        new UpdateOptions { IsUpsert = true });

      return result.IsAcknowledged;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "💥 Failed to update user data {UserId}", userId);
      return false;
    }
  }
}
