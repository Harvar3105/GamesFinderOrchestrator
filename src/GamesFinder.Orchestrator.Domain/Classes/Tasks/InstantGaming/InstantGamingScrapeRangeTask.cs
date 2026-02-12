using GamesFinder.Orchestrator.Domain.Enums;

namespace GamesFinder.Orchestrator.Domain.Classes.Tasks.InstantGaming;

public class InstantGamingScrapeRangeTask : InstantGamingScrapeTask
{
  public int StartId { get; set; } = 0;
  public int EndId { get; set; } = 0;

  public override string ToString()
  {
    return $"InstantGamingScrapeRangeTask: StartId: {StartId}, EndId: {EndId}, Currency: {Currency}, RedisKey: {RedisResultKey}";
  }
}