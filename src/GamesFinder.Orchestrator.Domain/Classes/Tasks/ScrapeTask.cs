namespace GamesFinder.Orchestrator.Domain.Classes.Tasks;

public abstract class ScrapeTask
{
  public Guid TaskId { get; set; } = Guid.NewGuid();
  public bool UpdateExistingDeals { get; set; } = false;
  public string RedisResultKey { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}