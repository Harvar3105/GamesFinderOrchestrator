using GamesFinder.Domain.Enums;
using GamesFinder.Domain.Interfaces.Repositories;
using GamesFinder.Orchestrator.Domain.Classes;
using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Classes.Tasks.InstantGaming;
using GamesFinder.Orchestrator.Domain.Interfaces.Infrastructure;
using GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;
using GamesFinder.Orchestrator.Publisher;
using Microsoft.Extensions.Logging;

namespace GamesFinder.Orchestrator.Services.ApplicationServices;

public class InstantGamingService : VendorsService<InstantGamingScrapeTask>, IInstantGamingService
{
  private readonly WorkersOptions _workersOptions;
  private readonly IGameOfferRepository _offerRepository;
  public InstantGamingService(PublisherFactory factory, WorkersOptions workersOptions, IGameOfferRepository offerRepository, ILogger<InstantGamingService> logger)
    : base(factory.Create<InstantGamingScrapeTask>(), logger)
  {
    _offerRepository = offerRepository;
    _workersOptions = workersOptions;
  }

  public override string TaskRedisKeyPrefix => $"instantgaming:scrape:result:{Guid.NewGuid()}";

  public async Task PublishIdsScrapeTaskAsync(List<string> vendorsIds, bool updateExisting = false)
  {
    if (vendorsIds == null || vendorsIds.Count() == 0)
    {
      _logger.LogWarning("Steam ID list cannot be empty.");
      throw new ArgumentException("Steam ID list cannot be empty.");
    }

    var parsedVendorIds = ParseVendorIds(vendorsIds);

    List<long> selectedVendorIds = [];
    if (updateExisting)
    {
      selectedVendorIds = parsedVendorIds;
    }
    else
    {
      selectedVendorIds = await RemoveExistingVendorIds(parsedVendorIds);
    }

    var batchSize = GetBatchSize(selectedVendorIds.Count());
    PublishTask(selectedVendorIds, batchSize);
  }

  public async Task PublishRangeScrapeTaskAsync(int minId, int maxId, bool updateExisting = false)
  {
    if (minId < 0 || maxId < 0 || maxId <= minId)
    {
      _logger.LogWarning($"Invalid range for Instant Gaming scrape task: StartId={minId}, EndId={maxId}");
      throw new ArgumentException("Invalid range for Instant Gaming scrape task.");
    }

    var ids = Enumerable.Range(minId, maxId - minId + 1).ToList();
    List<long> selectedVendorIds = [];
    if (updateExisting)
    {
      selectedVendorIds = ids.Select(id => (long)id).ToList();
    }
    else
    {
      selectedVendorIds = await RemoveExistingVendorIds(ids.Select(id => (long)id).ToList());
    }

    var batchSize = GetBatchSize(selectedVendorIds.Count());
    PublishTask(selectedVendorIds, batchSize);
  }

  public async Task PublishUpToMaxIdScrapeTaskAsync(int maxId, bool updateExisting = false) // Max id is exclusive
  {
    if (maxId < 0)
    {
      _logger.LogWarning("Invalid upToId for Instant Gaming scrape task: UpToId={UpToId}", maxId);
      throw new ArgumentException("Invalid upToId for Instant Gaming scrape task.");
    }

    var ids = Enumerable.Range(0, maxId).ToList();
    List<long> selectedVendorIds = [];
    if (updateExisting)
    {
      selectedVendorIds = ids.Select(id => (long)id).ToList();
    }
    else
    {
      selectedVendorIds = await RemoveExistingVendorIds(ids.Select(id => (long)id).ToList());
    }

    var batchSize = GetBatchSize(selectedVendorIds.Count());
    PublishTask(selectedVendorIds, batchSize);
  }

  private int GetBatchSize(int totalItems)
  {
    if (_workersOptions.InstantGamingOverrideBatchSize.HasValue)
    {
      return _workersOptions.InstantGamingOverrideBatchSize.Value;
    }

    else return CalculateBatchSize(totalItems, _workersOptions.InstantGamingWorkerCount);
  }

  private async Task<List<long>> RemoveExistingVendorIds(List<long> vendorIds)
  {
    var existingVendorIds = await _offerRepository.GetAllVendorIds(EVendor.InstantGaming);
    return vendorIds.Where(id => !existingVendorIds.Contains(id)).ToList();
  }

  private async void PublishTask(List<long> vendorIds, int batchSize)
  {
    for (int i = 0; i < vendorIds.Count(); i += batchSize)
    {
      var task = new InstantGamingScrapeTask
      {
        VendorsIds = vendorIds.Skip(i).Take(batchSize).ToList(),
        RedisResultKey = TaskRedisKeyPrefix,
      };
      await BatchPublishAsync(task); 
    }
  }

  private List<long> ParseVendorIds(List<string> vendorsIds)
  {
    var parsedVendorIds = new List<long>();
    foreach (var id in vendorsIds)
    {
      if (long.TryParse(id, out var parsedId))
      {
        parsedVendorIds.Add(parsedId);
      }
      else
      {
        _logger.LogWarning("Invalid vendor ID: {VendorId}. Skipping.", id);
      }
    }
    return parsedVendorIds;
  }
}
