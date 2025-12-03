namespace GamesFinder.Orchestrator.Publisher.RabbitMQ;

public class RabbitMqConfig
{
  public string HostName { get; }
  public int Port { get; }
  public string DefaultQueue { get; }
  public string SteamRequestsQueue { get; }
  public string SteamResultsQueue { get; }
  public string InstantGamingRequestsQueue { get; }
  public string InstantGamingResultsQueue { get; }
  public string UserName { get; }
  public string Password { get; }

  public RabbitMqConfig(
    string hostName,
    int port,
    string defaultQueue,
    string steamRequestsQueue,
    string steamResultsQueue,
    string instantGamingRequestsQueue,
    string instantGamingResultsQueue,
    string userName,
    string password)
  {
    HostName = hostName;
    Port = port;
    DefaultQueue = defaultQueue;
    SteamRequestsQueue = steamRequestsQueue;
    SteamResultsQueue = steamResultsQueue;
    InstantGamingRequestsQueue = instantGamingRequestsQueue;
    InstantGamingResultsQueue = instantGamingResultsQueue;
    UserName = userName;
    Password = password;
  }
}