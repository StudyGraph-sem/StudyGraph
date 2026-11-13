using StudyGraph.Domain.Entities;

namespace StudyGraph.Infrastructure.Persistence.Repositories.Interfaces;

public interface IStudyTaskRepository
{
    StudyTask? GetById(Guid id);
}
