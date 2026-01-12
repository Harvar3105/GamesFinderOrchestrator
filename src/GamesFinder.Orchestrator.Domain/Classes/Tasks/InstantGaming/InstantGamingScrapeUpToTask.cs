using GamesFinder.Orchestrator.Domain.Enums;

namespace GamesFinder.Orchestrator.Domain.Classes.Tasks.InstantGaming;

public class InstantGamingScrapeUpToTask : InstantGamingScrapeTask
{
  public int UpToId {get; set;} = 0;
}