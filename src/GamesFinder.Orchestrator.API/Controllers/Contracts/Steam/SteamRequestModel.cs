using System.ComponentModel.DataAnnotations;

namespace GamesFinder.Orchestrator.API.Controllers.Contracts.Steam;

public sealed record SteamRequestModel
  {
    [Required]
    [MinLength(1)]
    public List<long> steamIds { get; init; } = new();
    public bool updateExisting { get; init; } = false;
  }