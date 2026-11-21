using System.ComponentModel.DataAnnotations;
using GamesFinder.Orchestrator.Domain.Enums;

namespace GamesFinder.Orchestrator.API.Controllers.Contracts.GameOffers;

// Identifies an offer of a vendor. Vendor is always required, plus exactly one of GameId or VendorId.
// Nullable so that a missing parameter fails validation instead of binding to Guid.Empty or the first enum value.
public sealed record GameOfferLookupRequest : IValidatableObject
{
  public Guid? GameId { get; init; }

  // Id of the offer in the vendor's system (Steam app id, InstantGaming product id), compared case-insensitively.
  public string? VendorId { get; init; }

  [Required]
  public EVendor? Vendor { get; init; }

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  {
    var hasGameId = GameId is not null;
    var hasVendorId = !string.IsNullOrWhiteSpace(VendorId);

    if (hasGameId == hasVendorId)
    {
      yield return new ValidationResult(
        $"Exactly one of {nameof(GameId)} or {nameof(VendorId)} must be provided.",
        [nameof(GameId), nameof(VendorId)]);
    }
  }
}
