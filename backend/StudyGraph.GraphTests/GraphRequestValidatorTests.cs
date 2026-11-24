using System.Globalization;
using StudyGraph.Application.Graph.DTOs.Requests;
using StudyGraph.Application.Graph.Validation;

namespace StudyGraph.GraphTests;

public class GraphRequestValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void RejectsMissingTitles(string? title)
    {
        Assert.Contains("title", GraphRequestValidator.Validate(new SubjectInput(title!, "private")));
        Assert.Contains("title", GraphRequestValidator.Validate(new RoadmapInput(title!)));
        Assert.Contains("title", GraphRequestValidator.Validate(new TopicInput(title!, 1m)));
    }

    [Fact]
    public void CountsUnicodeCharactersForTitleLimit()
    {
        var character = char.ConvertFromUtf32(0x10400);
        var max = string.Concat(Enumerable.Repeat(character, 200));
        var tooLong = max + character;
        Assert.Empty(GraphRequestValidator.Validate(new RoadmapInput(max)));
        Assert.Contains("title", GraphRequestValidator.Validate(new RoadmapInput(tooLong)));
        Assert.Contains("title", GraphRequestValidator.Validate(new SubjectInput(new string('a', 201), "private")));
        Assert.Contains("title", GraphRequestValidator.Validate(new TopicInput(new string('a', 201), 1m)));
    }

    [Theory]
    [InlineData("private")]
    [InlineData("public")]
    [InlineData("unlisted")]
    public void AcceptsVisibilityValues(string visibility)
    {
        Assert.Empty(GraphRequestValidator.Validate(new SubjectInput("Subject", visibility)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("PUBLIC")]
    [InlineData("unknown")]
    public void RejectsInvalidVisibility(string? visibility)
    {
        Assert.Contains("visibility", GraphRequestValidator.Validate(new SubjectInput("Subject", visibility!)));
    }

    [Theory]
    [InlineData("0.01", true)]
    [InlineData("999999.99", true)]
    [InlineData("1.230", true)]
    [InlineData("0", false)]
    [InlineData("-1", false)]
    [InlineData("1000000", false)]
    [InlineData("0.001", false)]
    public void ChecksWeightRangeAndPrecision(string value, bool valid)
    {
        var weight = decimal.Parse(value, CultureInfo.InvariantCulture);
        var errors = GraphRequestValidator.Validate(new TopicInput("Topic", weight));
        Assert.Equal(valid, !errors.ContainsKey("importanceWeight"));
    }

    [Theory]
    [InlineData("0", true)]
    [InlineData("-999999999.999", true)]
    [InlineData("999999999.999", true)]
    [InlineData("1000000000", false)]
    [InlineData("-1000000000", false)]
    [InlineData("0.0001", false)]
    public void ChecksBothCoordinates(string value, bool valid)
    {
        var coordinate = decimal.Parse(value, CultureInfo.InvariantCulture);
        var errors = GraphRequestValidator.Validate(new TopicInput("Topic", 1m, PositionX: coordinate, PositionY: coordinate));
        Assert.Equal(valid, !errors.ContainsKey("positionX"));
        Assert.Equal(valid, !errors.ContainsKey("positionY"));
    }

    [Fact]
    public void AllowsMissingOptionalFieldsAndRequiresPositiveLevel()
    {
        Assert.Empty(GraphRequestValidator.Validate(new TopicInput("Topic", 1m)));
        Assert.Empty(GraphRequestValidator.Validate(new TopicInput("Topic", 1m, Level: 1)));
        Assert.Contains("level", GraphRequestValidator.Validate(new TopicInput("Topic", 1m, Level: 0)));
        Assert.Contains("level", GraphRequestValidator.Validate(new TopicInput("Topic", 1m, Level: -1)));
    }

    [Theory]
    [InlineData("not_started", true)]
    [InlineData("in_progress", true)]
    [InlineData("completed", true)]
    [InlineData(null, false)]
    [InlineData("completed ", false)]
    [InlineData("blocked", false)]
    public void ChecksTopicStatus(string? status, bool valid)
    {
        var errors = GraphRequestValidator.Validate(new TopicStatusInput(status!));
        Assert.Equal(valid, !errors.ContainsKey("studyStatus"));
    }

    [Fact]
    public void RequiresBothLinkIds()
    {
        var parent = Guid.NewGuid();
        var child = Guid.NewGuid();
        Assert.Empty(GraphRequestValidator.Validate(new TopicLinkInput(parent, child)));
        var errors = GraphRequestValidator.Validate(new TopicLinkInput(Guid.Empty, Guid.Empty));
        Assert.Contains("parentTopicId", errors);
        Assert.Contains("childTopicId", errors);
        Assert.Single(GraphRequestValidator.Validate(new TopicLinkInput(Guid.Empty, child)));
        Assert.Single(GraphRequestValidator.Validate(new TopicLinkInput(parent, Guid.Empty)));
    }

    [Fact]
    public void ReportsAllInvalidTopicFields()
    {
        var request = new TopicInput(" ", 0, PositionX: 0.0001m, PositionY: 1000000000m, Level: 0);
        var errors = GraphRequestValidator.Validate(request);
        Assert.Equal(5, errors.Count);
        Assert.Contains("title", errors);
        Assert.Contains("importanceWeight", errors);
        Assert.Contains("positionX", errors);
        Assert.Contains("positionY", errors);
        Assert.Contains("level", errors);
    }

    [Fact]
    public void RejectsNullRequests()
    {
        Assert.Throws<ArgumentNullException>(() => GraphRequestValidator.Validate((SubjectInput)null!));
        Assert.Throws<ArgumentNullException>(() => GraphRequestValidator.Validate((RoadmapInput)null!));
        Assert.Throws<ArgumentNullException>(() => GraphRequestValidator.Validate((TopicInput)null!));
        Assert.Throws<ArgumentNullException>(() => GraphRequestValidator.Validate((TopicStatusInput)null!));
        Assert.Throws<ArgumentNullException>(() => GraphRequestValidator.Validate((TopicLinkInput)null!));
    }
}
