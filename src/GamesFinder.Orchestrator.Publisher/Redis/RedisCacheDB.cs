using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Newtonsoft.Json;

namespace GamesFinder.Orchestrator.Publisher.Redis;

public class RedisCacheDB
{
  private readonly IDatabase _db;
  private ILogger<RedisCacheDB> _logger;

  public RedisCacheDB(IConnectionMultiplexer redis, ILogger<RedisCacheDB> logger)
  {
    _db = redis.GetDatabase();
    _logger = logger;
  }

  public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
  {
    var json = JsonConvert.SerializeObject(value);
    await _db.StringSetAsync(key, json, expiry);
  }

  public async Task<T?> GetAsync<T>(string key)
  {
    var json = await _db.StringGetAsync(key);
    return json.HasValue
    ? JsonConvert.DeserializeObject<T>(json.ToString())
    : default;
  }

  public async Task<IEnumerable<T>?> ListRangeAsync<T>(string key)
  {
    var json = await _db.ListRangeAsync(key);
    var result = new List<T>();

    foreach (var item in json)
    {
      if (item.HasValue)
      {
        try
        {
          var deserializedItem = JsonConvert.DeserializeObject<T>(
            item.ToString(),
            new JsonSerializerSettings
            {
              NullValueHandling = NullValueHandling.Ignore,
              MissingMemberHandling = MissingMemberHandling.Ignore
            });

          if (deserializedItem != null)
          {
              result.Add(deserializedItem);
          }
        }
        catch (JsonException ex)
        {
          _logger.LogError(ex, $"Failed to deserialize item from Redis. Item: {item}");
        }
      }
    }
    _logger.LogInformation($"Deserialized {result.Count()} items from Redis with key: {key}");

    return result;
  }
  
  public async Task<bool> ClearKey(string key)
  {
    return await _db.KeyDeleteAsync(key);
  }
}

