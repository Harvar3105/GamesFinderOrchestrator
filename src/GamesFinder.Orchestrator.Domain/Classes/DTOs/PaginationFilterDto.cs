using GamesFinder.Orchestrator.Domain.Enums;

namespace GamesFinder.Orchestrator.Domain.Classes.DTOs;

public sealed record PaginationFilterDto
{
  public int Page { get; init; }
  public int PageSize { get; init; }
  public string? Query { get; init; }
  public decimal? MinPrice { get; init; }
  public decimal? MaxPrice { get; init; }
  public ESort? Sort { get; init; }
  public bool? SteamAvailable { get; init; }
  public bool? InstantGamingAvailable { get; init; }
  public bool? G2aAvailable { get; init; }
}
