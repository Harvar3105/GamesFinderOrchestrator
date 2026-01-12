using GamesFinder.Orchestrator.Domain.Enums;

namespace GamesFinder.Orchestrator.Domain.Classes.Tasks.InstantGaming;

public class InstantGamingScrapeIdsTask : InstantGamingScrapeTask
{
  public List<string> GameIds { get; set; } = new();
}