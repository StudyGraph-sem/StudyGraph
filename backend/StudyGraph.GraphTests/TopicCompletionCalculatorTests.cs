using StudyGraph.Application.Graph.Services.Implementations;
using StudyGraph.Domain.Entities.Graph;

namespace StudyGraph.GraphTests;

public class TopicCompletionCalculatorTests
{
    private readonly TopicCompletionCalculator calculator = new();
    private readonly Guid roadmap = Guid.NewGuid();

    private Topic NewTopic(StudyStatus status = StudyStatus.NotStarted) =>
        new() { Id = Guid.NewGuid(), RoadmapId = roadmap, Title = "Topic", ImportanceWeight = 1, StudyStatus = status };

    private TopicDependency Link(Topic parent, Topic child) =>
        new() { RoadmapId = roadmap, PrerequisiteTopicId = parent.Id, TopicId = child.Id };

    [Fact]
    public void EmptyRoadmapIsNotCompleted()
    {
        var result = calculator.Calculate(roadmap, [], []);
        Assert.Empty(result.CompletedTopics);
        Assert.False(result.IsRoadmapCompleted);
    }

    [Theory]
    [InlineData(StudyStatus.NotStarted, false)]
    [InlineData(StudyStatus.InProgress, false)]
    [InlineData(StudyStatus.Completed, true)]
    public void LeafUsesItsOwnStatus(StudyStatus status, bool expected)
    {
        var topic = NewTopic(status);
        var result = calculator.Calculate(roadmap, [topic], []);
        Assert.Equal(expected, result.CompletedTopics[topic.Id]);
        Assert.Equal(expected, result.IsRoadmapCompleted);
    }

    [Fact]
    public void ParentNeedsAllChildrenRegardlessOfItsStoredStatus()
    {
        var parent = NewTopic(StudyStatus.Completed);
        var first = NewTopic(StudyStatus.Completed);
        var second = NewTopic(StudyStatus.InProgress);
        Topic[] topics = [second, parent, first]; TopicDependency[] links = [Link(parent, first), Link(parent, second)];
        var incomplete = calculator.Calculate(roadmap, topics, links);
        Assert.True(incomplete.CompletedTopics[first.Id]);
        Assert.False(incomplete.CompletedTopics[second.Id]);
        Assert.False(incomplete.CompletedTopics[parent.Id]);
        Assert.False(incomplete.IsRoadmapCompleted);
        Assert.Equal(StudyStatus.Completed, parent.StudyStatus);
        second.StudyStatus = StudyStatus.Completed;
        parent.StudyStatus = StudyStatus.NotStarted;
        var complete = calculator.Calculate(roadmap, topics, links);
        Assert.True(complete.CompletedTopics[parent.Id]);
        Assert.True(complete.IsRoadmapCompleted);
        Assert.Equal(StudyStatus.NotStarted, parent.StudyStatus);
    }

    [Fact]
    public void CompletionPropagatesThroughMultipleLevelsAndIndependentBranches()
    {
        var root = NewTopic(); var branch = NewTopic(); var leaf = NewTopic(StudyStatus.Completed);
        var other = NewTopic(StudyStatus.NotStarted);
        Topic[] topics = [leaf, other, root, branch]; TopicDependency[] links = [Link(branch, leaf), Link(root, branch)];
        var result = calculator.Calculate(roadmap, topics, links);
        Assert.True(result.CompletedTopics[leaf.Id]);
        Assert.True(result.CompletedTopics[branch.Id]);
        Assert.True(result.CompletedTopics[root.Id]);
        Assert.False(result.CompletedTopics[other.Id]);
        Assert.False(result.IsRoadmapCompleted);
        other.StudyStatus = StudyStatus.Completed;
        Assert.True(calculator.Calculate(roadmap, topics, links).IsRoadmapCompleted);
    }

    [Fact]
    public void RejectsInvalidTopicsAndLinks()
    {
        var a = NewTopic(); var b = NewTopic(); var c = NewTopic(); var link = Link(a, b);
        Assert.Throws<ArgumentException>(() => calculator.Calculate(roadmap, [a, a], []));
        Assert.Throws<ArgumentException>(() => calculator.Calculate(roadmap, [a], [link]));
        Assert.Throws<ArgumentException>(() => calculator.Calculate(roadmap, [a, b], [link, link]));
        Assert.Throws<ArgumentException>(() => calculator.Calculate(roadmap, [a, b, c], [link, Link(c, b)]));
        Assert.Throws<ArgumentException>(() => calculator.Calculate(roadmap, [a], [Link(a, a)]));
        Assert.Throws<ArgumentException>(() => calculator.Calculate(roadmap, [a, b], [link, Link(b, a)]));
        b.RoadmapId = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => calculator.Calculate(roadmap, [a, b], []));
        b.RoadmapId = roadmap; b.StudyStatus = (StudyStatus)100;
        Assert.Throws<ArgumentException>(() => calculator.Calculate(roadmap, [a, b], []));
    }

    [Fact]
    public void RejectsNullInputs()
    {
        Assert.Throws<ArgumentNullException>(() => calculator.Calculate(roadmap, null!, []));
        Assert.Throws<ArgumentNullException>(() => calculator.Calculate(roadmap, [], null!));
    }

    [Fact]
    public void LongChainDoesNotDependOnRecursion()
    {
        var topics = Enumerable.Range(0, 5000).Select(_ => NewTopic()).ToArray();
        topics[^1].StudyStatus = StudyStatus.Completed;
        var links = topics.Zip(topics.Skip(1), Link).Reverse().ToArray();
        var result = calculator.Calculate(roadmap, topics.Reverse().ToArray(), links);
        Assert.True(result.CompletedTopics[topics[0].Id]);
        Assert.True(result.IsRoadmapCompleted);
    }
}
