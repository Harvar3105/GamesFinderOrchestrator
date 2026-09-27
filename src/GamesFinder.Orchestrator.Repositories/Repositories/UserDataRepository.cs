using GamesFinder.Orchestrator.Domain.Classes;
using GamesFinder.Orchestrator.Domain.Interfaces.Repositories;
using GamesFinder.Orchestrator.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace GamesFinder.Orchestrator.Repositories.Repositories;

public class UserDataRepository : Repository<UserData>, IUserDataRepository
{
  public UserDataRepository(IMongoDatabase database, ILogger<Repository<UserData>> logger) : base(database, "users_data", logger)
  {
  }

}