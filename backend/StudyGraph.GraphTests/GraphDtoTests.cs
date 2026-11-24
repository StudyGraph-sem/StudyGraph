using System.Text.Json;
using StudyGraph.Application.Graph.DTOs.Requests;
using StudyGraph.Application.Graph.DTOs.Responses;

namespace StudyGraph.GraphTests;

public class GraphDtoTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void SubjectAndRoadmapRequestsReadApiFields()
    {
        var subject = JsonSerializer.Deserialize<SubjectInput>(
            """{"title":"Backend","visibility":"private","description":"Course"}""", Options);
        var roadmap = JsonSerializer.Deserialize<RoadmapInput>("""{"title":"Basics"}""", Options);

        Assert.NotNull(subject);
        Assert.Equal("Backend", subject.Title);
        Assert.Equal("private", subject.Visibility);
        Assert.Equal("Course", subject.Description);
        Assert.NotNull(roadmap);
        Assert.Equal("Basics", roadmap.Title);
        Assert.Null(roadmap.Description);
    }

    [Fact]
    public void TopicRequestReadsLevelCoordinatesAndWeight()
    {
        var request = JsonSerializer.Deserialize<TopicInput>(
            """{"title":"Kafka","importanceWeight":1.25,"positionX":-12.345,"positionY":10.125,"level":3}""", Options);

        Assert.NotNull(request);
        Assert.Equal("Kafka", request.Title);
        Assert.Equal(1.25m, request.ImportanceWeight);
        Assert.Equal(-12.345m, request.PositionX);
        Assert.Equal(10.125m, request.PositionY);
        Assert.Equal(3, request.Level);
        Assert.Null(request.Description);
    }

    [Fact]
    public void StatusAndLinkRequestsUseApiFieldNames()
    {
        var parent = Guid.NewGuid();
        var child = Guid.NewGuid();
        var link = JsonSerializer.Deserialize<TopicLinkInput>(
            $$"""{"parentTopicId":"{{parent}}","childTopicId":"{{child}}"}""", Options);
        var status = JsonSerializer.Deserialize<TopicStatusInput>("""{"studyStatus":"completed"}""", Options);

        Assert.NotNull(link);
        Assert.Equal(parent, link.ParentTopicId);
        Assert.Equal(child, link.ChildTopicId);
        Assert.Equal("completed", status?.StudyStatus);
    }

    [Fact]
    public void SubjectResponseDoesNotExposeOwnerOrShareToken()
    {
        var response = new SubjectResponse(Guid.NewGuid(), "Databases", "unlisted");
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(response, Options));
        var root = json.RootElement;

        Assert.Equal("Databases", root.GetProperty("title").GetString());
        Assert.Equal("unlisted", root.GetProperty("visibility").GetString());
        Assert.False(root.TryGetProperty("ownerUserId", out _));
        Assert.False(root.TryGetProperty("shareToken", out _));
    }

    [Fact]
    public void RoadmapResponseIncludesSubjectId()
    {
        var roadmapId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var response = new RoadmapResponse(roadmapId, subjectId, "Course");
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(response, Options));

        Assert.Equal(roadmapId, json.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(subjectId, json.RootElement.GetProperty("subjectId").GetGuid());
        Assert.Equal("Course", json.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public void GraphResponseIncludesTopicsLinksAndCompletion()
    {
        var roadmapId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var parent = new TopicResponse(parentId, roadmapId, "Backend", 1m, "not_started", true, Level: 1);
        var child = new TopicResponse(childId, roadmapId, "Kafka", 2m, "completed", true, Level: 2);
        var link = new TopicLinkResponse(roadmapId, parentId, childId);
        var response = new RoadmapGraphResponse(roadmapId, [parent, child], [link], true);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(response, Options));
        var root = json.RootElement;

        Assert.Equal(roadmapId, root.GetProperty("roadmapId").GetGuid());
        Assert.True(root.GetProperty("isCompleted").GetBoolean());
        var topics = root.GetProperty("topics");
        Assert.Equal(2, topics.GetArrayLength());
        Assert.Equal("completed", topics[1].GetProperty("studyStatus").GetString());
        Assert.True(topics[1].GetProperty("isCompleted").GetBoolean());
        Assert.Equal(2, topics[1].GetProperty("level").GetInt32());
        var edge = root.GetProperty("links")[0];
        Assert.Equal(roadmapId, edge.GetProperty("roadmapId").GetGuid());
        Assert.Equal(parentId, edge.GetProperty("parentTopicId").GetGuid());
        Assert.Equal(childId, edge.GetProperty("childTopicId").GetGuid());
    }
}
