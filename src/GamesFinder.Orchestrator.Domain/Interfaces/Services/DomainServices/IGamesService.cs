using System;
using GamesFinder.Orchestrator.Domain.Classes.Entities;

namespace GamesFinder.Orchestrator.Domain.Interfaces.DomainServices;

public interface IGamesService : IService<Game>
{
  // Task<long> ScrapIdsAsync(IEnumerable<int> gamesIds, bool updateExisting = false);
  Task<IEnumerable<int>> GetAllGamesSteamIdsAsync();
  // Task<long> UpdateSteamOffersAsync(IEnumerable<int> gamesIds);

  Task<bool> CheckIfSteamIdExistsAsync(int steamId);
  Task<Game?> GetBySteamIdAsync(int steamId);
  Task<string?> GetIdBySteamIdAsync(int steamId);
}
