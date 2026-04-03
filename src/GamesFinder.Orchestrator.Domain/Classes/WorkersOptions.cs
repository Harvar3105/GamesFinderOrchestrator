namespace GamesFinder.Orchestrator.Domain.Classes;

public class WorkersOptions
{
  public int InstantGamingWorkerCount { get; }
  public int? InstantGamingOverrideBatchSize { get; }

  public WorkersOptions(int instantGamingWorkerCount, int? instantGamingOverrideBatchSize)
  {
    InstantGamingWorkerCount = instantGamingWorkerCount;
    InstantGamingOverrideBatchSize = instantGamingOverrideBatchSize;
  }
}