
using GamesFinder.Orchestrator.Domain.Classes.Entities;

namespace GamesFinder.Domain.Interfaces.Repositories;

public interface IGameRepository : IRepository<Game>
{
	Task<Game?> GetBySteamIdAsync(long steamId);
	Task<Game?> GetByAppNameAsync(string appName);
	Task<Guid?> GetIdBySteamIdAsync(long steamId);
	Task<List<(long, bool)>> ExistBySteamIdMany(List<long> steamIds);
	Task<bool> ExistsBySteamIdAsync(long steamId);
	Task<bool> ExistsByAppNameAsync(string appName);
	Task<IEnumerable<long>> GetAllSteamIdsAsync();
	Task<int> GetTotalGamesCountAsync();
}