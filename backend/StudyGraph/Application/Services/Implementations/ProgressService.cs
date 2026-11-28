using StudyGraph.Application.DTOs.Requests;
using StudyGraph.Application.DTOs.Responses;
using StudyGraph.Application.Services.Interfaces;
namespace StudyGraph.Application.Services.Implementations;

public sealed class ProgressService : IProgressService
{
    public RoadmapProgressResponse CalculateWeightedProgress(Guid userId, UserRoadmapProgressSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(snapshot.Topics);
        ArgumentNullException.ThrowIfNull(snapshot.CompletedTopicIds);
        if (userId == Guid.Empty || snapshot.UserId == Guid.Empty || snapshot.RoadmapId == Guid.Empty)
            throw new ArgumentException("User and roadmap identifiers must not be empty.");
        if (snapshot.UserId != userId)
            throw new UnauthorizedAccessException("Progress belongs to another user.");

        var topics = snapshot.Topics.ToArray();
        var topicIds = new HashSet<Guid>();
        decimal totalWeight = 0;
        foreach (var topic in topics)
        {
            if (topic is null || topic.Id == Guid.Empty ||
                topic.RoadmapId != snapshot.RoadmapId || !topicIds.Add(topic.Id))
                throw new ArgumentException("Topics must have unique identifiers and belong to the requested roadmap.", nameof(snapshot));
            if (topic.ImportanceWeight <= 0 || topic.ImportanceWeight > 999999.99m ||
                decimal.Round(topic.ImportanceWeight, 2) != topic.ImportanceWeight)
                throw new ArgumentException("Topic weights must satisfy the graph contract: positive, at most 999999.99, with at most two decimal places.", nameof(snapshot));
            totalWeight += topic.ImportanceWeight;
        }

        var completedIds = snapshot.CompletedTopicIds.ToHashSet();
        if (!completedIds.IsSubsetOf(topicIds))
            throw new ArgumentException("Completed topics must exist in the supplied roadmap.", nameof(snapshot));

        var completedWeight = topics.Where(topic => completedIds.Contains(topic.Id)).Sum(topic => topic.ImportanceWeight);
        var percentage = totalWeight == 0 ? 0 : Math.Clamp(completedWeight / totalWeight * 100m, 0m, 100m);
        return new RoadmapProgressResponse(
            userId, snapshot.RoadmapId, percentage, totalWeight, completedWeight,
            topics.Length, completedIds.Count,
            Array.AsReadOnly(topics.Where(topic => completedIds.Contains(topic.Id)).Select(topic => topic.Id).ToArray()),
            Array.AsReadOnly(topics.Where(topic => !completedIds.Contains(topic.Id)).Select(topic => topic.Id).ToArray()));
    }
}
