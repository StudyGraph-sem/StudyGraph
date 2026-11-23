using StudyGraph.Application.Graph.Services.Implementations;
using StudyGraph.Domain.Entities.Graph;

namespace StudyGraph.GraphTests;

public class TopicHierarchyValidatorTests
{
    private readonly TopicHierarchyValidator validator = new();
    private readonly Guid roadmap = Guid.NewGuid();

    private Topic NewTopic() => new() { Id = Guid.NewGuid(), RoadmapId = roadmap, Title = "Topic", ImportanceWeight = 1 };
    private TopicDependency Link(Topic parent, Topic child) => new() { RoadmapId = roadmap, PrerequisiteTopicId = parent.Id, TopicId = child.Id };

    [Fact]
    public void AllowsMultipleChildrenAndIndependentTrees()
    {
        var a = NewTopic(); var b = NewTopic(); var c = NewTopic(); var d = NewTopic();
        Topic[] topics = [a, b, c, d]; TopicDependency[] links = [Link(a, b)];
        Assert.True(validator.Validate(Link(a, c), topics, links).IsValid);
        Assert.True(validator.Validate(Link(c, d), topics, links).IsValid);
        Assert.Single(links);
        Assert.Equal(StudyStatus.NotStarted, a.StudyStatus);
    }

    [Fact]
    public void ReportsMissingTopicsInOrder()
    {
        var a = NewTopic(); var b = NewTopic(); var link = Link(a, b);
        var noTopics = validator.Validate(link, [], []);
        Assert.Equal(TopicHierarchyError.TopicNotFound, noTopics.Error);
        Assert.Equal(b.Id, noTopics.MissingTopicId);
        var missingParent = validator.Validate(link, [b], []);
        Assert.Equal(TopicHierarchyError.TopicNotFound, missingParent.Error);
        Assert.Equal(a.Id, missingParent.MissingTopicId);
    }

    [Fact]
    public void RejectsDifferentRoadmapsSelfLinkDuplicateAndSecondParent()
    {
        var a = NewTopic(); var b = NewTopic(); var c = NewTopic();
        Topic[] topics = [a, b, c]; var link = Link(a, b);
        Assert.Equal(TopicHierarchyError.SelfReference, validator.Validate(Link(a, a), topics, []).Error);
        Assert.Equal(TopicHierarchyError.DuplicateLink, validator.Validate(link, topics, [link]).Error);
        Assert.Equal(TopicHierarchyError.MultipleParents, validator.Validate(Link(c, b), topics, [link]).Error);
        var wrongRoadmap = Link(a, b); wrongRoadmap.RoadmapId = Guid.NewGuid();
        Assert.Equal(TopicHierarchyError.RoadmapMismatch, validator.Validate(wrongRoadmap, topics, []).Error);
        b.RoadmapId = Guid.NewGuid();
        Assert.Equal(TopicHierarchyError.RoadmapMismatch, validator.Validate(link, topics, []).Error);
    }

    [Fact]
    public void RejectsDirectAndLongCycles()
    {
        var a = NewTopic(); var b = NewTopic(); var c = NewTopic();
        Topic[] topics = [a, b, c];
        Assert.Equal(TopicHierarchyError.Cycle, validator.Validate(Link(b, a), topics, [Link(a, b)]).Error);
        Assert.Equal(TopicHierarchyError.Cycle, validator.Validate(Link(c, a), topics, [Link(b, c), Link(a, b)]).Error);
    }

    [Fact]
    public void RejectsInvalidExistingGraph()
    {
        var a = NewTopic(); var b = NewTopic(); var c = NewTopic(); var link = Link(a, b);
        Assert.Throws<ArgumentException>(() => validator.Validate(link, [a, a, b], []));
        Assert.Throws<ArgumentException>(() => validator.Validate(link, [a], [link]));
        Assert.Throws<ArgumentException>(() => validator.Validate(link, [a, b], [link, link]));
        Assert.Throws<ArgumentException>(() => validator.Validate(link, [a, b, c], [link, Link(c, b)]));
        Assert.Throws<ArgumentException>(() => validator.Validate(link, [a, b], [Link(a, a)]));
        Assert.Throws<ArgumentException>(() => validator.Validate(link, [a, b], [link, Link(b, a)]));
    }

    [Fact]
    public void RejectsNullInputs()
    {
        var a = NewTopic(); var b = NewTopic(); var link = Link(a, b);
        Assert.Throws<ArgumentNullException>(() => validator.Validate(null!, [a, b], []));
        Assert.Throws<ArgumentNullException>(() => validator.Validate(link, null!, []));
        Assert.Throws<ArgumentNullException>(() => validator.Validate(link, [a, b], null!));
    }

    [Fact]
    public void HandlesLongChainsAndUnorderedInputs()
    {
        var topics = Enumerable.Range(0, 5000).Select(_ => NewTopic()).ToArray();
        var links = topics.Zip(topics.Skip(1), Link).Reverse().ToArray();
        Assert.Equal(TopicHierarchyError.Cycle, validator.Validate(Link(topics[^1], topics[0]), topics.Reverse().ToArray(), links).Error);
    }

    [Fact]
    public void PreservesInputAndIgnoresUnrelatedRoadmap()
    {
        var a = NewTopic(); var b = NewTopic(); var c = NewTopic();
        var x = NewTopic(); var y = NewTopic();
        var otherRoadmap = Guid.NewGuid(); x.RoadmapId = otherRoadmap; y.RoadmapId = otherRoadmap;
        var other = new TopicDependency { RoadmapId = otherRoadmap, PrerequisiteTopicId = x.Id, TopicId = y.Id };
        var first = Link(a, b);
        Topic[] topics = [y, b, a, c, x]; TopicDependency[] links = [other, first];
        Assert.True(validator.Validate(Link(a, c), topics, links).IsValid);
        Assert.Equal(TopicHierarchyError.MultipleParents, validator.Validate(Link(c, b), topics, links).Error);
        Assert.Equal(new[] { other, first }, links);
        Assert.Equal(a.Id, first.PrerequisiteTopicId);
        Assert.Equal(b.Id, first.TopicId);
    }

    [Fact]
    public void RejectsNullElementsAndLinksFromAnotherRoadmap()
    {
        var a = NewTopic(); var b = NewTopic(); var link = Link(a, b);
        Assert.Throws<ArgumentException>(() => validator.Validate(link, [a, null!, b], []));
        Assert.Throws<ArgumentException>(() => validator.Validate(link, [a, b], [null!]));
        var wrongRoadmap = Link(a, b); wrongRoadmap.RoadmapId = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => validator.Validate(link, [a, b], [wrongRoadmap]));
    }

    [Fact]
    public void DetectsCycleInExistingGraphEvenWhenCandidateIsUnrelated()
    {
        var a = NewTopic(); var b = NewTopic(); var c = NewTopic(); var d = NewTopic();
        var error = Assert.Throws<ArgumentException>(() =>
            validator.Validate(Link(c, d), [a, b, c, d], [Link(a, b), Link(b, a)]));
        Assert.Equal("links", error.ParamName);
    }
}
