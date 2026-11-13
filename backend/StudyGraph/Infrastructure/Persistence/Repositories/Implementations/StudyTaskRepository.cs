using StudyGraph.Domain.Entities;
using StudyGraph.Infrastructure.Persistence.Repositories.Interfaces;

namespace StudyGraph.Infrastructure.Persistence.Repositories.Implementations;

public class StudyTaskRepository : IStudyTaskRepository
{
    public StudyTask? GetById(Guid id) => throw new NotImplementedException();
}
