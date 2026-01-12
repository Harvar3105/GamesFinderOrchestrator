namespace GamesFinder.Orchestrator.Domain.Classes.Tasks;

public class SteamScrapeTask : ScrapeTask
{ 
  public List<string> GameIds { get; set; } = new();
  public bool UpdateExistingGames {get; set;} = false;
}