using System.ComponentModel.DataAnnotations;
using GamesFinder.Orchestrator.Domain.Enums;

namespace GamesFinder.Orchestrator.API.Controllers.Contracts.UserData;

public sealed record UpdatePreferencesRequest
{
  [Required]
  public ECurrency PreferredCurrency { get; init; }

  // ISO 3166-1 alpha-2 country code
  [RegularExpression("^[A-Za-z]{2}$")]
  public string? Region { get; init; }
}
