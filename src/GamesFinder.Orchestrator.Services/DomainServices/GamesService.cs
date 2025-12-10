using GamesFinder.Domain.Interfaces.Repositories;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Interfaces.DomainServices;

namespace GamesFinder.Orchestrator.Services.DomainServices;

public class GamesService : Service<Game, IGameRepository>, IGamesService
{
  public GamesService(IGameRepository repository) : base(repository)
  {
    
  }

  public Task<bool> CheckIfSteamIdExistsAsync(int steamId)
  {
    return _repository.ExistsBySteamIdAsync(steamId);
  }

  public Task<IEnumerable<int>> GetAllGamesSteamIdsAsync()
  {
    return _repository.GetAllSteamIdsAsync();
  }

  public Task<Game?> GetBySteamIdAsync(int steamId)
  {
    return _repository.GetBySteamId(steamId);
  }
}