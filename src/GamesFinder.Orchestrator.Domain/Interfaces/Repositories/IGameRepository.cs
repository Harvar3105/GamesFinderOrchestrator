
using GamesFinder.Orchestrator.Domain.Classes.Entities;

namespace GamesFinder.Domain.Interfaces.Repositories;

public interface IGameRepository : IRepository<Game>
{
	Task<Game?> GetBySteamIdAsync(int steamId);
	Task<Game?> GetByAppNameAsync(string appName);
	Task<string?> GetIdBySteamIdAsync(int steamId);
	Task<List<(int, bool)>> ExistBySteamIdMany(List<int> steamIds);
	Task<bool> ExistsBySteamIdAsync(int steamId);
	Task<(bool, string)> GetExistBySteamIdAsync(int steamId);
	Task<bool> ExistsByAppNameAsync(string appName);
	Task<IEnumerable<int>> GetAllSteamIdsAsync();
}