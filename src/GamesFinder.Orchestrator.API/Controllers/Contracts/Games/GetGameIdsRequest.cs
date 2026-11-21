using System.ComponentModel.DataAnnotations;

namespace GamesFinder.Orchestrator.API.Controllers.Contracts.Games;

// Exactly one lookup key must be provided.
public sealed record GetGameIdsRequest : IValidatableObject
{
  public long? SteamId { get; init; }

  // Matches when the given name contains the name of a stored game (case-insensitive); the shortest stored name wins.
  public string? Name { get; init; }

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  {
    var hasSteamId = SteamId is not null;
    var hasName = !string.IsNullOrWhiteSpace(Name);

    if (hasSteamId == hasName)
    {
      yield return new ValidationResult(
        $"Exactly one of {nameof(SteamId)} or {nameof(Name)} must be provided.",
        [nameof(SteamId), nameof(Name)]);
    }
  }
}
