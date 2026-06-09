using GamesFinder.Orchestrator.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GamesFinder.Orchestrator.Domain.Classes.Entities;

public class Game(
  string name,
  long steamID,
  List<GameOffer>? initialOffers = null,
  string? description = null,
  string? steamUrl = null,
  string? headerImage = null,
  Game.GameStoreMetadata? storeMetadata = null
  ) : Entity
{
  [BsonElement("name")]
  public string Name { get; set; } = name;

  [BsonElement("steam_url")]
  public string? SteamURL { get; set; } = steamUrl;
  [BsonElement("steam_id")]
  public long SteamID { get; set; } = steamID;
  [BsonElement("in_packages")]
	public List<long> InPackages { get; set; } = new();
	[BsonElement("is_DLC")]
	public bool IsDLC { get; set; }
  [BsonElement("description")]
  public string? Description { get; set; } = description;
  [BsonElement("header_image")]
  public string? HeaderImage { get; set; } = headerImage;
  [BsonIgnore]
  public List<GameOffer> Offers { get; set; } = initialOffers ?? new();
  [BsonElement("is_released")]
  public bool IsReleased { get; set; }
  [BsonElement("initial_price")]
  public decimal? InitialPrice {get; set;}
  [BsonElement("initial_currency")]
  public ECurrency? InitialCurrency {get; set;}
  [BsonElement("store_metadata")]
  public GameStoreMetadata? StoreMetadata { get; set; } = storeMetadata;

  public override string ToString()
  {
    return "🎮:\n" + base.ToString() + $"Name: {Name}, SteamID: {SteamID}, Offers count: {Offers?.Count()},\nDescription: {Description?.Substring(0, Math.Min(50, Description.Length))}..., SteamURL: {SteamURL}\nMetadata: {StoreMetadata}";
  }

  public record GameStoreMetadata
  {
    [BsonElement("tags")]
    public IEnumerable<string> Tags { get; init; } = [];
    [BsonElement("genres")]
    public IEnumerable<string> Genres { get; init; } = [];
    [BsonElement("positive_reviews")]
    public int PositiveReviews { get; init; }
    [BsonElement("negative_reviews")]
    public int NegativeReviews { get; init; }
    [BsonElement("positive_reviews_percent")]
    public double PositiveReviewsPercent { get; init; }

    public override string ToString()
    {
      return $"Tags: {string.Join(", ", Tags)}, Genres: {string.Join(", ", Genres)}, PositiveReviews: {PositiveReviews}, NegativeReviews: {NegativeReviews}, PositiveReviewsPercent: {PositiveReviewsPercent}%";
    }
  }
}
