using GamesFinder.Orchestrator.Domain.Enums;

namespace GamesFinder.Orchestrator.Domain.Classes.Tasks.InstantGaming;

public abstract class InstantGamingScrapeTask : ScrapeTask
{
  public ECurrency Currency { get; set; } = ECurrency.EUR;
}