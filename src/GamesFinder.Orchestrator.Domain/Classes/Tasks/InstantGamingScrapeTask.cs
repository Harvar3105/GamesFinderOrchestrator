using GamesFinder.Orchestrator.Domain.Enums;

namespace GamesFinder.Orchestrator.Domain.Classes.Tasks.InstantGaming;

public class InstantGamingScrapeTask : ScrapeTask
{
  public IEnumerable<long> VendorsIds {get; set;} = [];
  public ECurrency Currency { get; set; } = ECurrency.EUR;
}