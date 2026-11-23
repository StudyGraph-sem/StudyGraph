using StudyGraph.Application.Graph.Validation;
using StudyGraph.Domain.Entities.Graph;

namespace StudyGraph.Application.Graph.Services.Interfaces;

public interface IGraphDependencyService
{
    DependencyValidationResult ValidateDependency(
        TopicDependency candidate,
        IReadOnlyCollection<Topic> topics,
        IReadOnlyCollection<TopicDependency> dependencies);

    IReadOnlyDictionary<Guid, bool> GetBlockedTopics(
        IReadOnlyCollection<Topic> topics,
        IReadOnlyCollection<TopicDependency> dependencies);
}
