using GamesFinder.Orchestrator.Domain.Classes.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GamesFinder.Orchestrator.Domain.Classes;

public class UserData(
  Guid userId,
  List<int> usersWishlist,
  string? pfpName,
  byte[]? pfpContent,
  string? pfpExtension
) : Entity
{
  [BsonId]
  [BsonRepresentation(BsonType.String)]
  [BsonElement("user_id")]
  public new Guid Id { get; set; } = userId;
  [BsonElement("wishlist")]
  public List<int> UsersWishlist { get; set; } = usersWishlist ?? [];
  [BsonElement("pfp_name")]
  public string? PFPName { get; set; } = pfpName;
  [BsonElement("pfp_content")]
  public byte[]? PFPContent { get; set; } = pfpContent;
  [BsonElement("pfp_extension")]
  public string? PFPExtension { get; set; } = pfpExtension;
}