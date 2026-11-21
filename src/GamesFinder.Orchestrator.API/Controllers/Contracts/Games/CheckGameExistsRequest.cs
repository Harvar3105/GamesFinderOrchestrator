using System.ComponentModel.DataAnnotations;

namespace GamesFinder.Orchestrator.API.Controllers.Contracts.Games;

// Exactly one lookup key must be provided.
public sealed record CheckGameExistsRequest : IValidatableObject
{
  public Guid? Id { get; init; }

  public long? SteamId { get; init; }

  // Matches when the given name contains the name of a stored game (case-insensitive).
  public string? Name { get; init; }

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  {
    var providedKeysCount = (Id is null ? 0 : 1)
      + (SteamId is null ? 0 : 1)
      + (string.IsNullOrWhiteSpace(Name) ? 0 : 1);

    if (providedKeysCount != 1)
    {
      yield return new ValidationResult(
        $"Exactly one of {nameof(Id)}, {nameof(SteamId)} or {nameof(Name)} must be provided.",
        [nameof(Id), nameof(SteamId), nameof(Name)]);
    }
  }
}
