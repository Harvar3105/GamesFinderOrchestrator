namespace GamesFinder.Orchestrator.Domain.Classes;

public class SteamOptions
{
  public string DomainName { get; }
  public string ApiKey { get; }
  public int MaxRequests { get; }
  public int TagsRquestsDelay { get; }
  public int CooldownMilliseconds { get; }

  public SteamOptions(string domainName, string apiKey, int maxRequests, int tagsRquestsDelay, int cooldownMilliseconds)
  {
    DomainName = domainName;
    ApiKey = apiKey;
    MaxRequests = maxRequests;
    TagsRquestsDelay = tagsRquestsDelay;
    CooldownMilliseconds = cooldownMilliseconds;
  }
}