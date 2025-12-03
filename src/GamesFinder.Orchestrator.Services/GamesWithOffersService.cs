using GamesFinder.Domain.Interfaces.Repositories;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Interfaces.Services;

namespace GamesFinder.Orchestrator.Services;

public class GamesWithOffersService(IGameRepository<Game> gameRepository, IGameOfferRepository<GameOffer> gameOfferRepository) : IGamesWithOffersService<Entity>
{
  protected readonly IGameRepository<Game> _gameRepository = gameRepository;
  protected readonly IGameOfferRepository<GameOffer> _gameOfferRepository = gameOfferRepository;
  public async Task<long> CountAsync()
  {
    return await _gameRepository.CountAsync();
  }

  public async Task<bool> DeleteAsync(Guid id)
  {
    var game = await _gameRepository.DeleteAsync(id);
    if (!game) return false;

    var offersList = await _gameOfferRepository.GetByGameIdAsync(id);
    if (offersList is not null && offersList.Any())
    {
      await _gameOfferRepository.DeleteManyAsync(offersList.Select(o => o.Id));
    }
    return true;
  }

  public async Task<bool> DeleteManyAsync(IEnumerable<Guid> ids)
  {
    var games = await _gameRepository.DeleteManyAsync(ids);
    if (games == 0) return false;

    var offersList = await _gameOfferRepository.GetByGameIdsAsync(ids);
    if (offersList is not null && offersList.Any())
    {
      await _gameOfferRepository.DeleteManyAsync(offersList.Values.Select(offersList => offersList.Select(offer => offer.Id)).SelectMany(id => id));
    }
    return true;
  }

  public async Task<bool> ExistsAsync(Guid id)
  {
    return await _gameRepository.ExistsAsync(id);
  }

  public async Task<ICollection<Game>?> GetAllAsync()
  {
    var games = await _gameRepository.GetAllAsync();
    if (games is null || !games.Any()) return games;

    var gamesOffers = await _gameOfferRepository.GetByGameIdsAsync(games.Select(g => g.Id));
    if (gamesOffers is null || !gamesOffers.Any()) return games;

    foreach (var game in games)
    {
      if (gamesOffers.TryGetValue(game.Id, out var offers))
      {
        game.Offers = offers;
      }
    }
    return games; 
  }

  public async Task<Game?> GetByIdAsync(Guid id)
  {
    var game = await _gameRepository.GetByIdAsync(id);
    if (game is null) return null;

    var offers = await _gameOfferRepository.GetByGameIdAsync(id);
    if (offers is not null && offers.Any())
    {
      game.Offers = [.. offers];
    }
    return game;
  }

  public async Task<ICollection<Game>?> GetPagedAsync(int page, int pageSize)
  {
    var games = await _gameRepository.GetPagedAsync(page, pageSize);
    if (games is null || !games.Any()) return null;

    var gamesOffers = await _gameOfferRepository.GetByGameIdsAsync(games.Select(g => g.Id));
    if (gamesOffers is null || !gamesOffers.Any()) return games;

    foreach (var game in games)
    {
      if (gamesOffers.TryGetValue(game.Id, out var offers))
      {
        game.Offers = offers;
      }
    }
    return games;
  }

  public async Task<bool> SaveAsync(Entity entity)
  {
    if (entity is Game game)
    {
      var offers = await _gameOfferRepository.SaveManyAsync(game.Offers);
      if (!offers) return false;
      var returnGame = await _gameRepository.SaveAsync(game);
      return returnGame;
    }
    else if (entity is GameOffer offer)
    {
      await _gameOfferRepository.SaveAsync(offer);
      return true;
    }
    else return false;
  }

  public async Task<bool> SaveManyAsync(IEnumerable<Entity> entities)
  {
    if (entities.All(e => e is Game))
    {
      var games = entities.Cast<Game>();
      var offers = await _gameOfferRepository.SaveManyAsync(games.SelectMany(e => e.Offers));
      if (!offers) return false;
      var savedGames = await _gameRepository.SaveManyAsync(games);
      return savedGames;
    }
    else if (entities.All(e => e is GameOffer))
    {
      var offers = entities.Cast<GameOffer>();
      var savedOffers =  await _gameOfferRepository.SaveManyAsync(offers);
      return savedOffers;
    }
    else return false;
  }

  public async Task<bool> SaveOrUpdateAsync(Game entity)
  {
    var offers = await _gameOfferRepository.SaveOrUpdateManyAsync(entity.Offers);
    if (!offers) return false;
    var game = await _gameRepository.SaveOrUpdateAsync(entity);
    return game;
  }

  public async Task<bool> SaveOrUpdateManyAsync(IEnumerable<Game> entities)
  {
    var offers = await _gameOfferRepository.SaveOrUpdateManyAsync(entities.SelectMany(e => e.Offers));
    if (!offers) return false;
    var games = await _gameRepository.SaveOrUpdateManyAsync(entities);
    return games;
  }

  public async Task<bool> UpdateAsync(Game entity)
  {
    var offers = await _gameOfferRepository.SaveOrUpdateManyAsync(entity.Offers); //TODO: Add Update Many? May cause lots of requests
    if (!offers) return false;
    var game = await _gameRepository.UpdateAsync(entity);
    return game;
  }
}
