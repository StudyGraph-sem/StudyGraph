using StudyGraph.Application.DTOs.Requests;
using StudyGraph.Application.Services.Implementations;
using StudyGraph.Domain.Entities.Graph;
namespace StudyGraph.GraphTests;

public sealed class ProgressServiceTests
{
    private readonly Guid userId = Guid.NewGuid();
    private readonly Guid roadmapId = Guid.NewGuid();
    private readonly ProgressService service = new();
    private Topic Topic(decimal weight) => new() { Id = Guid.NewGuid(), RoadmapId = roadmapId, Title = "Topic", ImportanceWeight = weight };
    private UserRoadmapProgressSnapshot Snapshot(Topic[] topics, params Guid[] completed) => new(userId, roadmapId, topics, completed);

    [Fact]
    public void NoCompletedTopicsReturnsZero()
    {
        var result = service.CalculateWeightedProgress(userId, Snapshot([Topic(2), Topic(3)]));
        Assert.Equal(0m, result.Percentage);
        Assert.Equal(0, result.CompletedTopicCount);
        Assert.Equal(2, result.IncompleteTopicIds.Count);
    }
    [Fact]
    public void AllCompletedTopicsReturnsHundred()
    {
        var topics = new[] { Topic(1), Topic(9) };
        var result = service.CalculateWeightedProgress(userId, Snapshot(topics, topics.Select(topic => topic.Id).ToArray()));
        Assert.Equal(100m, result.Percentage);
        Assert.Equal(2, result.CompletedTopicCount);
        Assert.Empty(result.IncompleteTopicIds);
    }
    [Fact]
    public void DifferentWeightsProduceWeightedPercentage()
    {
        var topics = new[] { Topic(1), Topic(3) };
        var result = service.CalculateWeightedProgress(userId, Snapshot(topics, topics[0].Id));
        Assert.Equal(25m, result.Percentage);
        Assert.Equal(4m, result.TotalWeight);
        Assert.Equal(1m, result.CompletedWeight);
        Assert.Equal(topics[0].Id, Assert.Single(result.CompletedTopicIds));
        Assert.Equal(topics[1].Id, Assert.Single(result.IncompleteTopicIds));
    }
    [Fact]
    public void DecimalWeightsDoNotRequireRoundingOrDefaultWeights()
    {
        var topics = new[] { Topic(0.01m), Topic(0.03m) };
        Assert.Equal(25m, service.CalculateWeightedProgress(userId, Snapshot(topics, topics[0].Id)).Percentage);
    }
    [Fact]
    public void EmptyGraphReturnsZeroWithoutDivision()
    {
        var result = service.CalculateWeightedProgress(userId, Snapshot([]));
        Assert.Equal(0m, result.Percentage);
        Assert.Equal(0m, result.TotalWeight);
        Assert.Equal(0, result.TotalTopicCount);
    }
    [Fact]
    public void UsersHaveIndependentCompletionAndGlobalTopicStatusIsIgnored()
    {
        var topic = Topic(5);
        topic.StudyStatus = StudyStatus.Completed;
        var first = service.CalculateWeightedProgress(userId, Snapshot([topic], topic.Id));
        var otherUserId = Guid.NewGuid();
        var second = service.CalculateWeightedProgress(otherUserId, new(otherUserId, roadmapId, [topic], []));
        Assert.Equal(100m, first.Percentage);
        Assert.Equal(0m, second.Percentage);
        Assert.Equal(StudyStatus.Completed, topic.StudyStatus);
    }
    [Fact]
    public void RejectsAnotherUsersSnapshot()
    {
        Assert.Throws<UnauthorizedAccessException>(() => service.CalculateWeightedProgress(Guid.NewGuid(), Snapshot([Topic(1)])));
    }
    [Fact]
    public void RepeatedCompletionDoesNotCountWeightTwice()
    {
        var topic = Topic(1);
        var result = service.CalculateWeightedProgress(userId, Snapshot([topic], topic.Id, topic.Id));
        Assert.Equal(100m, result.Percentage);
        Assert.Equal(1, result.CompletedTopicCount);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000000)]
    [InlineData(0.001)]
    public void RejectsInvalidWeights(double weight)
    {
        Assert.Throws<ArgumentException>(() => service.CalculateWeightedProgress(userId, Snapshot([Topic((decimal)weight)])));
    }
    [Fact]
    public void RejectsMissingTopicsAndCompletionData()
    {
        Assert.Throws<ArgumentNullException>(() => service.CalculateWeightedProgress(userId, null!));
        Assert.Throws<ArgumentNullException>(() => service.CalculateWeightedProgress(userId, new(userId, roadmapId, null!, [])));
        Assert.Throws<ArgumentNullException>(() => service.CalculateWeightedProgress(userId, new(userId, roadmapId, [], null!)));
        Assert.Throws<ArgumentException>(() => service.CalculateWeightedProgress(userId, Snapshot([], Guid.NewGuid())));
        Assert.Throws<ArgumentException>(() => service.CalculateWeightedProgress(userId, Snapshot([null!])));
    }
    [Fact]
    public void RejectsDuplicateTopicsAndMismatchedRoadmap()
    {
        var topic = Topic(1);
        Assert.Throws<ArgumentException>(() => service.CalculateWeightedProgress(userId, Snapshot([topic, topic])));
        topic.RoadmapId = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => service.CalculateWeightedProgress(userId, Snapshot([topic])));
    }
    [Fact]
    public void RejectsEmptyIdentifiers()
    {
        Assert.Throws<ArgumentException>(() => service.CalculateWeightedProgress(Guid.Empty, new(Guid.Empty, roadmapId, [], [])));
        Assert.Throws<ArgumentException>(() => service.CalculateWeightedProgress(userId, new(userId, Guid.Empty, [], [])));
        var topic = Topic(1);
        topic.Id = Guid.Empty;
        Assert.Throws<ArgumentException>(() => service.CalculateWeightedProgress(userId, Snapshot([topic])));
    }
}
