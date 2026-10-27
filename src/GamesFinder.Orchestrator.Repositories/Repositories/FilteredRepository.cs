using GamesFinder.Orchestrator.API.Controllers.Contracts.Steam;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Enums;
using GamesFinder.Orchestrator.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GamesFinder.Orchestrator.Repositories.Repositories;

// Read-only repository for queries that span the `games` and `game_offers` collections.
public class FilteredRepository : IFilteredRepository
{
  // TODO: Create single code source point for entity and repository?
  private const string GamesCollectionName = "games";
  private const string OffersCollectionName = "game_offers";
  private const string LowestPriceField = "lowest_price";
  private const string HasPriceField = "has_price";

  private readonly IMongoCollection<Game> _gamesCollection;
  private readonly ILogger<FilteredRepository> _logger;

  public FilteredRepository(IMongoDatabase database, ILogger<FilteredRepository> logger)
  {
    _gamesCollection = database.GetCollection<Game>(GamesCollectionName);
    _logger = logger;
  }

  public async Task<IEnumerable<Game>> GetPagedWithFiltersAsync(int page, int pageSize, GamesFilter filterDto, ECurrency currency = ECurrency.USD)
  {
    try
    {
      var stages = BuildFilterStages(filterDto, currency);

      stages.Add(new BsonDocument("$sort", BuildSort(filterDto.Sort)));
      stages.Add(new BsonDocument("$skip", (page - 1) * pageSize));
      stages.Add(new BsonDocument("$limit", pageSize));
      stages.Add(new BsonDocument("$unset", new BsonArray { LowestPriceField, HasPriceField }));

      return await _gamesCollection
        .Aggregate(PipelineDefinition<Game, Game>.Create(stages))
        .ToListAsync();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting paged games with filters");
      return [];
    }
  }

  public async Task<int> GetTotalCountWithFiltersAsync(GamesFilter filterDto, ECurrency currency = ECurrency.USD)
  {
    try
    {
      var stages = BuildFilterStages(filterDto, currency);
      stages.Add(new BsonDocument("$count", "count"));

      var result = await _gamesCollection
        .Aggregate(PipelineDefinition<Game, BsonDocument>.Create(stages))
        .FirstOrDefaultAsync();

      return result?["count"].ToInt32() ?? 0;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error counting games with filters");
      return 0;
    }
  }

  private static List<BsonDocument> BuildFilterStages(GamesFilter filterDto, ECurrency currency)
  {
    var stages = new List<BsonDocument>();

    var gameMatch = new BsonDocument();
    if (filterDto.Tags.Count > 0)
      gameMatch.Add("store_metadata.tags", new BsonDocument("$all", new BsonArray(filterDto.Tags)));
    if (filterDto.Genres.Count > 0)
      gameMatch.Add("store_metadata.genres", new BsonDocument("$all", new BsonArray(filterDto.Genres)));
    if (gameMatch.ElementCount > 0)
      stages.Add(new BsonDocument("$match", gameMatch));

    var filtersByOffers = filterDto.Min.HasValue || filterDto.Max.HasValue || filterDto.Vendor.HasValue;
    var sortsByPrice = filterDto.Sort is ESort.priceAsc or ESort.priceDesc;
    if (!filtersByOffers && !sortsByPrice)
      return stages;

    var offerMatch = new BsonDocument
    {
      { "$expr", new BsonDocument("$eq", new BsonArray { "$game_id", "$$gameId" }) },
      { "currency", currency.ToString() },
      { "amount", new BsonDocument("$ne", BsonNull.Value) },
    };
    if (filterDto.Vendor.HasValue)
      offerMatch.Add("vendor", filterDto.Vendor.Value.ToString());

    stages.Add(new BsonDocument("$lookup", new BsonDocument
    {
      { "from", OffersCollectionName },
      { "let", new BsonDocument("gameId", "$_id") },
      { "pipeline", new BsonArray
        {
          new BsonDocument("$match", offerMatch),
          new BsonDocument("$group", new BsonDocument
          {
            { "_id", BsonNull.Value },
            { "min", new BsonDocument("$min", new BsonDocument("$toDecimal", "$amount")) },
          }),
        }
      },
      { "as", LowestPriceField },
    }));

    stages.Add(new BsonDocument("$addFields", new BsonDocument
    {
      { LowestPriceField, new BsonDocument("$first", $"${LowestPriceField}.min") },
    }));
    stages.Add(new BsonDocument("$addFields", new BsonDocument
    {
      { HasPriceField, new BsonDocument("$ne", new BsonArray { new BsonDocument("$type", $"${LowestPriceField}"), "missing" }) },
    }));

    if (filtersByOffers)
    {
      var priceMatch = new BsonDocument { { HasPriceField, true } };
      var priceRange = new BsonDocument();
      if (filterDto.Min.HasValue)
        priceRange.Add("$gte", new BsonDecimal128(filterDto.Min.Value));
      if (filterDto.Max.HasValue)
        priceRange.Add("$lte", new BsonDecimal128(filterDto.Max.Value));
      if (priceRange.ElementCount > 0)
        priceMatch.Add(LowestPriceField, priceRange);

      stages.Add(new BsonDocument("$match", priceMatch));
    }

    return stages;
  }

  // `_id` is the last key so that pages stay stable when sort values are equal.
  private static BsonDocument BuildSort(ESort sort)
  {
    var sortDocument = sort switch
    {
      ESort.newest => new BsonDocument("createdAt", -1),
      ESort.oldest => new BsonDocument("createdAt", 1),
      ESort.newestUpdated => new BsonDocument("updatedAt", -1),
      ESort.oldestUpdated => new BsonDocument("updatedAt", 1),
      ESort.name => new BsonDocument("name", 1),
      ESort.nameReversed => new BsonDocument("name", -1),
      // Games without a price in the requested currency go first in ascending order and last in descending.
      ESort.priceAsc => new BsonDocument { { HasPriceField, 1 }, { LowestPriceField, 1 } },
      ESort.priceDesc => new BsonDocument { { HasPriceField, -1 }, { LowestPriceField, -1 } },
      _ => new BsonDocument("updatedAt", -1),
    };

    sortDocument.Add("_id", 1);
    return sortDocument;
  }
}
