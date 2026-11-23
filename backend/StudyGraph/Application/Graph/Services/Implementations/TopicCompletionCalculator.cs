using System.Collections.ObjectModel;
using StudyGraph.Domain.Entities.Graph;

namespace StudyGraph.Application.Graph.Services.Implementations;

public sealed record TopicCompletionResult(
    IReadOnlyDictionary<Guid, bool> CompletedTopics,
    bool IsRoadmapCompleted);

public sealed class TopicCompletionCalculator
{
    public TopicCompletionResult Calculate(
        Guid roadmapId,
        IReadOnlyCollection<Topic> topics,
        IReadOnlyCollection<TopicDependency> links)
    {
        ArgumentNullException.ThrowIfNull(topics);
        ArgumentNullException.ThrowIfNull(links);

        var topicsById = new Dictionary<Guid, Topic>();
        foreach (var topic in topics)
        {
            if (topic is null || topic.RoadmapId != roadmapId ||
                !Enum.IsDefined(topic.StudyStatus) || !topicsById.TryAdd(topic.Id, topic))
                throw new ArgumentException("Topics must be unique, belong to the roadmap and have valid statuses.", nameof(topics));
        }

        var parentByTopic = new Dictionary<Guid, Guid>();
        var remainingChildren = topicsById.Keys.ToDictionary(id => id, _ => 0);
        foreach (var link in links)
        {
            if (link is null || link.RoadmapId != roadmapId || link.TopicId == link.PrerequisiteTopicId ||
                !topicsById.ContainsKey(link.TopicId) || !topicsById.ContainsKey(link.PrerequisiteTopicId) ||
                !parentByTopic.TryAdd(link.TopicId, link.PrerequisiteTopicId))
                throw new ArgumentException("Links must connect topics in the roadmap with at most one parent per topic.", nameof(links));
            remainingChildren[link.PrerequisiteTopicId]++;
        }

        var completed = new Dictionary<Guid, bool>();
        var allChildrenCompleted = topicsById.Keys.ToDictionary(id => id, _ => true);
        var pending = new Queue<Guid>();
        foreach (var topic in topicsById.Values)
        {
            if (remainingChildren[topic.Id] != 0) continue;
            completed[topic.Id] = topic.StudyStatus == StudyStatus.Completed;
            pending.Enqueue(topic.Id);
        }

        while (pending.TryDequeue(out var topicId))
        {
            if (!parentByTopic.TryGetValue(topicId, out var parentId)) continue;
            allChildrenCompleted[parentId] &= completed[topicId];
            remainingChildren[parentId]--;
            if (remainingChildren[parentId] != 0) continue;
            completed[parentId] = allChildrenCompleted[parentId];
            pending.Enqueue(parentId);
        }

        if (completed.Count != topicsById.Count)
            throw new ArgumentException("Links must not contain cycles.", nameof(links));

        return new TopicCompletionResult(
            new ReadOnlyDictionary<Guid, bool>(completed),
            completed.Count > 0 && completed.Values.All(value => value));
    }
}
