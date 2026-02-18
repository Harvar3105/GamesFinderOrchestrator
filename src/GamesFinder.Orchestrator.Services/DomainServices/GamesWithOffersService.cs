using GamesFinder.Domain.Interfaces.Repositories;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Interfaces.DomainServices;

namespace GamesFinder.Orchestrator.Services.DomainServices;

public class GamesWithOffersService : IGamesWithOffersService
{
  private readonly IGameRepository _gamesRepo;
  private readonly IGameOfferRepository _offersRepo;

  public GamesWithOffersService(IGameRepository gamesRepository, IGameOfferRepository offersRepository)
  {
    _gamesRepo = gamesRepository;
    _offersRepo = offersRepository;
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
    foreach (var game in games)
    {
      var offers = await _offersRepo.GetByGameIdAsync(game.Id);
      game.Offers = offers?.ToList() ?? new List<GameOffer>();
    }
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

  // public Task<ICollection<Entity>?> GetPagedAsync(int page, int pageSize)
  // {
  //   throw new NotImplementedException();
  // }

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
