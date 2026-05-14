using GamesFinder.Orchestrator.Domain.Classes.Tasks;

namespace GamesFinder.Orchestrator.Domain.Interfaces.Infrastructure;

public interface IPublisher<in TTask> where TTask : ScrapeTask
{
  Task PublishIdsScrapeTaskAsync(TTask task);
}