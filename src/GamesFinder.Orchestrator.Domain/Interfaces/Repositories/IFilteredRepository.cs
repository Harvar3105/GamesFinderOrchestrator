using GamesFinder.Orchestrator.API.Controllers.Contracts.Steam;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Enums;

namespace GamesFinder.Orchestrator.Domain.Interfaces.Repositories;

// Read-only queries that filter games by data from several collections (games and their offers).
public interface IFilteredRepository
{
	Task<IEnumerable<Game>> GetPagedWithFiltersAsync(int page, int pageSize, GamesFilter filterDto, ECurrency currency = ECurrency.USD);
	Task<int> GetTotalCountWithFiltersAsync(GamesFilter filterDto, ECurrency currency = ECurrency.USD);
}
