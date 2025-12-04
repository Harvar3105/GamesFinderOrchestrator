using System;
using GamesFinder.Orchestrator.Domain.Classes.Entities;

namespace GamesFinder.Orchestrator.Domain.Interfaces.Services;

public interface IGamesService : IService<Game>
{
  Task<long> ScrapIdsAsync(IEnumerable<int> gamesIds, bool updateExisting = false);
  Task<IEnumerable<int>> GetAllGamesSteamIdsAsync();
  Task<long> UpdateSteamOffersAsync(IEnumerable<int> gamesIds);

  Task<(bool, string)> CheckIfSteamIdExistsAsync(int steamId);
}
