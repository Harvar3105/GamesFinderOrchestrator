using GamesFinder.Orchestrator.Domain.Classes.Tasks;

namespace GamesFinder.Orchestrator.Domain.Interfaces.Infrastructure;

public interface IPublisher
{
  Task PublishIdsScrapeTaskAsync(ScrapeTask task);
}