using GamesFinder.Orchestrator.Domain.Enums;

namespace GamesFinder.Orchestrator.Domain.Classes.Tasks.InstantGaming;

public class InstantGamingScrapeUpToTask : InstantGamingScrapeTask
{
  public int UpToId {get; set;} = 0;

  public override string ToString()
  {
    return $"InstantGamingScrapeUpToTask: UpToId: {UpToId}, Currency: {Currency}, RedisKey: {RedisResultKey}";
  }
}