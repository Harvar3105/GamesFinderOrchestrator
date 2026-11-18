namespace GamesFinder.Orchestrator.Domain.Classes;

public class CacheOptions
{
  public TimeSpan TagsDuration { get; }

  public CacheOptions(int tagsMinutes)
  {
    if (tagsMinutes <= 0)
      throw new ArgumentOutOfRangeException(nameof(tagsMinutes), tagsMinutes, "Cache:TagsMinutes must be a positive number of minutes.");

    TagsDuration = TimeSpan.FromMinutes(tagsMinutes);
  }
}
