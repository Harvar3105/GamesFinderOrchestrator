
using GamesFinder.Orchestrator.Domain.Classes.Entities;

namespace GamesFinder.Domain.Interfaces.Repositories;

public interface IGameRepository<TEntity> : IRepository<TEntity> where TEntity : Game
{
	Task<Game?> GetBySteamId(int steamId);
	Task<Game?> GetByAppNameAsync(string appName);
	Task<List<(int, bool)>> ExistBySteamIdMany(List<int> steamIds);
	Task<bool> ExistsBySteamIdAsync(int steamId);
	Task<(bool, string)> GetExistBySteamIdAsync(int steamId);
	Task<bool> ExistsByAppNameAsync(string appName);
	Task<IEnumerable<int>> GetAllSteamIdsAsync();
}