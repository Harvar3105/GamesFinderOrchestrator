using GamesFinder.Orchestrator.Domain.Classes.Entities;
using GamesFinder.Orchestrator.Domain.Enums;

namespace GamesFinder.Orchestrator.Domain.Interfaces.Repositories;

public interface IUserDataRepository : IRepository<UserData>
{
  Task<bool> LinkSteamAccountAsync(Guid userId, long steamId64);
  Task<bool> UnlinkSteamAccountAsync(Guid userId);
  Task<bool> ReplaceWishlistAsync(Guid userId, IEnumerable<WishlistItem> wishlist, EWishlistSyncStatus status);
  Task<bool> SetWishlistSyncStatusAsync(Guid userId, EWishlistSyncStatus status);
  Task<bool> HideGameAsync(Guid userId, long steamAppId);
  Task<bool> UnhideGameAsync(Guid userId, long steamAppId);
  Task<bool> UpdatePreferencesAsync(Guid userId, ECurrency preferredCurrency, string? region);
  Task<ICollection<UserData>> GetStaleWishlistsAsync(DateTime syncedBefore, int limit);
  Task<ICollection<long>> GetDistinctWishlistSteamAppIdsAsync();
}
