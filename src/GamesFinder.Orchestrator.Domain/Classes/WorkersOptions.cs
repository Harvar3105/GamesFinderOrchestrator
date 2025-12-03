namespace GamesFinder.Orchestrator.Domain.Classes;

public class WorkersOptions
{
  public int InstantGamingWorkerCount { get; }
  public int InstantGamingSkipFirstIds { get; }

  public WorkersOptions(int instantGamingWorkerCount, int instantGamingSkipFirstIds)
  {
    InstantGamingWorkerCount = instantGamingWorkerCount;
    InstantGamingSkipFirstIds = instantGamingSkipFirstIds;
  }
}