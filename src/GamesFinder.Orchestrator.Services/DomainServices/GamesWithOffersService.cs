using GamesFinder.Domain.Interfaces.Repositories;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Interfaces.DomainServices;

namespace GamesFinder.Orchestrator.Services.DomainServices;

public class GamesWithOffersService
{
  private readonly IGamesService _gamesService;
  private readonly IOffersService _offersService;

  public GamesWithOffersService(IGamesService gamesService, IOffersService offersService)
  {
    _gamesService = gamesService;
    _offersService = offersService;
  }

  public async Task<(bool, long)> DeleteAsync(Guid id)
  {
    var isGameDeleted = await _gamesService.DeleteAsync(id);
    if (!isGameDeleted) return (false, 0);
    var offers = await _offersService.DeleteByGameIdAsync(id);
    return (isGameDeleted, offers);
  }

  public async Task<(long, long)> DeleteManyAsync(IEnumerable<Guid> ids)
  {
    var isGameDeleted = await _gamesService.DeleteManyAsync(ids);
    int counter = 0;
    foreach (var id in ids)
    {
      var offersCount = await _offersService.DeleteByGameIdAsync(id);
      counter += (int)offersCount;
    }
    return (isGameDeleted, counter);
  }

  public async Task<ICollection<Game>?> GetAllAsync()
  {
    var games = await _gamesService.GetAllAsync();
    if (games == null) return null;
    foreach (var game in games)
    {
      var offers = await _offersService.GetOffersByGameIdAsync(game.Id);
      game.Offers = offers?.ToList() ?? new List<GameOffer>();
    }
    return games;
  }

  public async Task<Entity?> GetByIdAsync(Guid id)
  {
    var game = await _gamesService.GetByIdAsync(id);
    if (game == null) return null;

    var offers = await _offersService.GetOffersByGameIdAsync(id);
    game.Offers = offers?.ToList() ?? new List<GameOffer>();
    return game;
  }

  // public Task<ICollection<Entity>?> GetPagedAsync(int page, int pageSize)
  // {
  //   throw new NotImplementedException();
  // }

  public async Task<bool> SaveAsync(Game game)
  {
    var result = await _gamesService.SaveAsync(game);
    if (!result) return false;
    result = await _offersService.SaveManyAsync(game.Offers);
    return result;
  }

  public async Task<bool> SaveManyAsync(IEnumerable<Game> games)
  {
    var result = await _gamesService.SaveManyAsync(games);
    if (!result) return false;
    foreach (var game in games)
    {
      await _offersService.SaveManyAsync(game.Offers);
    }
    return true;
  }

  public async Task<bool> SaveOrUpdateAsync(Game game)
  {
    var result = await _gamesService.SaveOrUpdateAsync(game);
    if (!result) return false;
    result = await _offersService.SaveOrUpdateManyAsync(game.Offers);
    return result;
  }

  public async Task<bool> SaveOrUpdateManyAsync(IEnumerable<Game> games)
  {
    var result = await _gamesService.SaveOrUpdateManyAsync(games);
    if (!result) return false;
    foreach (var game in games)
    {
      await _offersService.SaveManyAsync(game.Offers);
    }
    return true;
  }
}
