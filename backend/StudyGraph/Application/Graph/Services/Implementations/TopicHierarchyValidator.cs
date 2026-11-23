using StudyGraph.Domain.Entities.Graph;

namespace StudyGraph.Application.Graph.Services.Implementations;

public enum TopicHierarchyError
{
    None,
    TopicNotFound,
    RoadmapMismatch,
    SelfReference,
    DuplicateLink,
    MultipleParents,
    Cycle
}

public sealed record TopicHierarchyResult(TopicHierarchyError Error, Guid? MissingTopicId = null)
{
    public bool IsValid => Error == TopicHierarchyError.None;
}

public sealed class TopicHierarchyValidator
{
    public TopicHierarchyResult Validate(
        TopicDependency candidate,
        IReadOnlyCollection<Topic> topics,
        IReadOnlyCollection<TopicDependency> links)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(topics);
        ArgumentNullException.ThrowIfNull(links);

        var topicsById = new Dictionary<Guid, Topic>();
        foreach (var topic in topics)
        {
            if (topic is null || !topicsById.TryAdd(topic.Id, topic))
                throw new ArgumentException("Topics must be non-null and have unique IDs.", nameof(topics));
        }

        var parentByTopic = new Dictionary<Guid, Guid>();
        foreach (var link in links)
        {
            if (link is null || link.TopicId == link.PrerequisiteTopicId ||
                !topicsById.TryGetValue(link.TopicId, out var child) ||
                !topicsById.TryGetValue(link.PrerequisiteTopicId, out var parent) ||
                child.RoadmapId != link.RoadmapId || parent.RoadmapId != link.RoadmapId ||
                !parentByTopic.TryAdd(link.TopicId, link.PrerequisiteTopicId))
                throw new ArgumentException("Existing links must connect topics in the same roadmap with at most one parent per topic.", nameof(links));
        }

        var checkedTopics = new HashSet<Guid>();
        foreach (var topicId in topicsById.Keys)
        {
            var path = new HashSet<Guid>();
            var current = topicId;
            while (!checkedTopics.Contains(current) && parentByTopic.TryGetValue(current, out var parentId))
            {
                if (!path.Add(current))
                    throw new ArgumentException("Existing links must not contain cycles.", nameof(links));
                current = parentId;
            }
            checkedTopics.UnionWith(path);
        }

        if (!topicsById.TryGetValue(candidate.TopicId, out var candidateChild))
            return new(TopicHierarchyError.TopicNotFound, candidate.TopicId);
        if (!topicsById.TryGetValue(candidate.PrerequisiteTopicId, out var candidateParent))
            return new(TopicHierarchyError.TopicNotFound, candidate.PrerequisiteTopicId);
        if (candidateChild.RoadmapId != candidate.RoadmapId || candidateParent.RoadmapId != candidate.RoadmapId)
            return new(TopicHierarchyError.RoadmapMismatch);
        if (candidate.TopicId == candidate.PrerequisiteTopicId)
            return new(TopicHierarchyError.SelfReference);
        if (parentByTopic.TryGetValue(candidate.TopicId, out var existingParent))
            return new(existingParent == candidate.PrerequisiteTopicId ? TopicHierarchyError.DuplicateLink : TopicHierarchyError.MultipleParents);

        var ancestor = candidate.PrerequisiteTopicId;
        while (parentByTopic.TryGetValue(ancestor, out var parent))
        {
            if (parent == candidate.TopicId)
                return new(TopicHierarchyError.Cycle);
            ancestor = parent;
        }

        return new(TopicHierarchyError.None);
    }
}
