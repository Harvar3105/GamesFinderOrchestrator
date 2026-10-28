using GamesFinder.Orchestrator.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GamesFinder.Orchestrator.Domain.Classes.Entities;

public class UserData : Entity
{
  [BsonElement("steam_id64")]
  public long? SteamId64 { get; set; }
  [BsonElement("wishlist_synced_at")]
  public DateTime? WishlistSyncedAt { get; set; }
  [BsonElement("wishlist_sync_status")]
  [BsonRepresentation(BsonType.String)]
  public EWishlistSyncStatus WishlistSyncStatus { get; set; } = EWishlistSyncStatus.NotSynced;
  [BsonElement("steam_wishlist")]
  public List<WishlistItem> SteamWishlist { get; set; } = [];
  [BsonElement("hidden_steam_ids")]
  public List<long> HiddenSteamIds { get; set; } = [];
  [BsonElement("preferred_currency")]
  public ECurrency PreferredCurrency { get; set; } = ECurrency.EUR;
  // ISO 3166-1 alpha-2 country code
  [BsonElement("region")]
  public string? Region { get; set; }

  public UserData() { }

  public UserData(Guid userId)
  {
    Id = userId;
  }
}

public class WishlistItem
{
  [BsonElement("steam_app_id")]
  public long SteamAppId { get; set; }
  [BsonElement("priority")]
  public int Priority { get; set; }
  [BsonElement("added_at")]
  public DateTime AddedAt { get; set; }
}
