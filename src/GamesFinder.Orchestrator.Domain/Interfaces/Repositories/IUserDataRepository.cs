using GamesFinder.Orchestrator.Domain.Classes;
using GamesFinder.Orchestrator.Domain.Interfaces.Repositories;

namespace GamesFinder.Orchestrator.Domain.Interfaces.Repositories;

public interface IUserDataRepository : IRepository<UserData>
{
  new Task<bool> SaveManyAsync(IEnumerable<UserData> entities)
  {
    throw new NotSupportedException();
  }
  new Task<bool> SaveOrUpdateManyAsync(IEnumerable<UserData> entities)
  {
    throw new NotSupportedException();
  }
  new Task<long> DeleteManyAsync(IEnumerable<Guid> ids)
  {
    throw new NotSupportedException();
  }
  new Task<ICollection<UserData>> GetAllAsync()
  {
    throw new NotSupportedException();
  }
  new Task<ICollection<UserData>?> GetPagedAsync(int page, int pageSize)
  {
    throw new NotSupportedException();
  }
}