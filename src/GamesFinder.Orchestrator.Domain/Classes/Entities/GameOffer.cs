using GamesFinder.Domain.Enums;
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

  public GameOffer(Guid gameId, EVendor vendor, string vendorsGameId, string vendorsUrl, decimal? amount, ECurrency? currency, bool available = false)
  {
    GameId = gameId;
    Vendor = vendor;
    VendorsGameId = vendorsGameId;
    VendorsUrl = vendorsUrl;
    Available = available;
    Amount = amount;
    Currency = currency;
  }
  
  //TODO: Configure Equals for object comparing

}