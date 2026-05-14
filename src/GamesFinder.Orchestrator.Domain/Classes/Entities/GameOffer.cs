using GamesFinder.Orchestrator.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace GamesFinder.Orchestrator.Domain.Classes.Entities;

public class GameOffer : Entity
{
  [BsonElement("game_id")]
  [BsonRepresentation(BsonType.String)]
  public Guid GameId { get; set; }
  [BsonElement("vendors_game_id")]
  public string VendorsGameId { get; set; }
  [BsonElement("vendor")]
  public EVendor Vendor { get; set; }
  [BsonElement("vendors_url")]
  public string VendorsUrl { get; set; }
  [BsonElement("available")]
  public bool Available { get; set; }
  [BsonElement("amount")]
  public decimal? Amount {get; set;}
  [BsonElement("currency")]
  public ECurrency? Currency {get; set;}
  [BsonElement("offer_name")]
  public string OfferName { get; set; }

  public GameOffer(Guid gameId, EVendor vendor, string vendorsGameId, string vendorsUrl, decimal? amount, ECurrency? currency, string offerName, bool available = false)
  {
    GameId = gameId;
    Vendor = vendor;
    VendorsGameId = vendorsGameId;
    VendorsUrl = vendorsUrl;
    Available = available;
    Amount = amount;
    Currency = currency;
    OfferName = offerName;
  }

  public override string ToString()
  {
    return "🫴:\n" + base.ToString() + $"GameID: {GameId}, Vendor: {Vendor}, VendorsGameId: {VendorsGameId},\nVendorsUrl: {VendorsUrl}, Available: {Available}, Amount: {Amount} {Currency}, Offer Name: {OfferName}\n";
  }
}