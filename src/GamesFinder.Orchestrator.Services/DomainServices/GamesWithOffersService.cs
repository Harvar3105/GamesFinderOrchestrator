using GamesFinder.Orchestrator.API.Controllers.Contracts.Steam;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Enums;
using GamesFinder.Orchestrator.Domain.Interfaces.DomainServices;
using GamesFinder.Orchestrator.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace GamesFinder.Orchestrator.Services.DomainServices;

public class GamesWithOffersService : IGamesWithOffersService
{
  private readonly IGameRepository _gamesRepo;
  private readonly IGameOfferRepository _offersRepo;
  private readonly IFilteredRepository _filteredRepo;
  private readonly ILogger<GamesWithOffersService> _logger;

  public GamesWithOffersService(
    IGameRepository gamesRepository,
    IGameOfferRepository offersRepository,
    IFilteredRepository filteredRepository,
    ILogger<GamesWithOffersService> logger)
  {
    _gamesRepo = gamesRepository;
    _offersRepo = offersRepository;
    _filteredRepo = filteredRepository;
    _logger = logger;
  }

  public async Task<(bool, long)> DeleteAsync(Guid id)
  {
    var isGameDeleted = await _gamesRepo.DeleteAsync(id);
    if (!isGameDeleted) return (false, 0);
    var offers = await _offersRepo.DeleteByGameIdAsync(id);
    return (isGameDeleted, offers);
  }

  public async Task<(long, long)> DeleteManyAsync(IEnumerable<Guid> ids)
  {
    var isGameDeleted = await _gamesRepo.DeleteManyAsync(ids);
    int counter = 0;
    foreach (var id in ids)
    {
      var offersCount = await _offersRepo.DeleteByGameIdAsync(id);
      counter += (int)offersCount;
    }
    return (isGameDeleted, counter);
  }

  public async Task<ICollection<Game>?> GetAllAsync()
  {
    var games = await _gamesRepo.GetAllAsync();
    if (games == null) return null;
    await AttachOffersAsync(games);
    return games;
  }

  public async Task<Entity?> GetByIdAsync(Guid id)
  {
    var game = await _gamesRepo.GetByIdAsync(id);
    if (game == null) return null;

    var offers = await _offersRepo.GetByGameIdAsync(id);
    game.Offers = offers?.ToList() ?? new List<GameOffer>();
    return game;
  }

  public async Task<IEnumerable<Game>> GetPagedAsync(int page, int pageSize, ECurrency currency = ECurrency.USD)
  {
    try
    {
      var games = await _gamesRepo.GetPagedAsync(page, pageSize);
      if (games == null || games.Count == 0) return [];

      await AttachOffersAsync(games, currency);
      return games;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting paged games");
      return [];
    }
  }

  public async Task<IEnumerable<Game>> GetPagedFilteredAsync(int page, int pageSize, GamesFilter filter, ECurrency currency = ECurrency.USD)
  {
    try
    {
      var games = (await _filteredRepo.GetPagedWithFiltersAsync(page, pageSize, filter, currency)).ToList();
      if (games.Count == 0) return [];

      await AttachOffersAsync(games, currency, filter.Vendor);
      return games;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting paged filtered games");
      return [];
    }
  }

  // Does not provide offers
  public async Task<IEnumerable<(Game, decimal?)>> GetPagedWithLowestAsync(int page, int pageSize, ECurrency currency = ECurrency.USD)
  {
    try
    {
      var games = await _gamesRepo.GetPagedAsync(page, pageSize);
      if (games == null || games.Count == 0) return [];

      var lowestPrices = (await _offersRepo.GetMinimalOfferPriceManyAsync(games.Select(g => g.Id), currency))
        .ToDictionary(p => p.Item1, p => p.Item2);

      return games.Select(game => (game, lowestPrices.GetValueOrDefault(game.Id))).ToList();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting paged games with lowest prices");
      return [];
    }
  }

  // Does not provide offers
  public async Task<IEnumerable<(Game, decimal?)>> GetPagedWithLowestFilteredAsync(int page, int pageSize, GamesFilter filter, ECurrency currency = ECurrency.USD)
  {
    try
    {
      var games = (await _filteredRepo.GetPagedWithFiltersAsync(page, pageSize, filter, currency)).ToList();
      if (games.Count == 0) return [];

      var lowestPrices = (await _offersRepo.GetMinimalOfferPriceManyAsync(games.Select(g => g.Id), currency, filter.Vendor))
        .ToDictionary(p => p.Item1, p => p.Item2);

      return games.Select(game => (game, lowestPrices.GetValueOrDefault(game.Id))).ToList();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting paged filtered games with lowest prices");
      return [];
    }
  }

  public async Task<int> GetTotalCountFilteredAsync(GamesFilter filter, ECurrency currency = ECurrency.USD)
  {
    return await _filteredRepo.GetTotalCountWithFiltersAsync(filter, currency);
  }

  private async Task AttachOffersAsync(IEnumerable<Game> games, ECurrency currency = ECurrency.USD, EVendor? vendor = null)
  {
    var offersByGame = await _offersRepo.GetByGameIdManyAsync(games.Select(g => g.Id));

    foreach (var game in games)
    {
      var offers = offersByGame?.GetValueOrDefault(game.Id) ?? [];
      game.Offers = offers
        .Where(o => o.Currency == currency && (!vendor.HasValue || o.Vendor == vendor.Value))
        .ToList();
    }
  }

  public async Task<bool> SaveAsync(Game game)
  {
    var result = await _gamesRepo.SaveAsync(game);
    if (!result) return false;
    result = await _offersRepo.SaveManyAsync(game.Offers);
    return result;
  }

  public async Task<bool> SaveManyAsync(IEnumerable<Game> games)
  {
    var result = await _gamesRepo.SaveManyAsync(games);
    if (!result) return false;
    foreach (var game in games)
    {
      await _offersRepo.SaveManyAsync(game.Offers);
    }
    return true;
  }

  public async Task<bool> SaveOrUpdateAsync(Game game)
  {
    var result = await _gamesRepo.SaveOrUpdateAsync(game);
    if (!result) return false;
    result = await _offersRepo.SaveOrUpdateManyAsync(game.Offers);
    return result;
  }

  public async Task<bool> SaveOrUpdateManyAsync(IEnumerable<Game> games)
  {
    var result = await _gamesRepo.SaveOrUpdateManyAsync(games);
    if (!result) return false;
    foreach (var game in games)
    {
      await _offersRepo.SaveManyAsync(game.Offers);
    }
    return true;
  }

}
