using System.Text;
using StudyGraph.Application.Graph.DTOs.Requests;

namespace StudyGraph.Application.Graph.Validation;

public static class GraphRequestValidator
{
    private const decimal MaxWeight = 999999.99m;
    private const decimal MaxCoordinate = 999999999.999m;

    public static IReadOnlyDictionary<string, string[]> Validate(SubjectInput request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new Dictionary<string, string[]>();
        ValidateTitle(request.Title, errors);
        if (request.Visibility is not ("private" or "public" or "unlisted"))
            errors["visibility"] = ["Visibility must be private, public or unlisted."];
        return errors;
    }

    public static IReadOnlyDictionary<string, string[]> Validate(RoadmapInput request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new Dictionary<string, string[]>();
        ValidateTitle(request.Title, errors);
        return errors;
    }

    public static IReadOnlyDictionary<string, string[]> Validate(TopicInput request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new Dictionary<string, string[]>();
        ValidateTitle(request.Title, errors);
        if (request.ImportanceWeight <= 0 || request.ImportanceWeight > MaxWeight ||
            decimal.Round(request.ImportanceWeight, 2) != request.ImportanceWeight)
            errors["importanceWeight"] = ["Importance weight must be positive, at most 999999.99 and have at most two decimal places."];
        ValidateCoordinate(request.PositionX, "positionX", errors);
        ValidateCoordinate(request.PositionY, "positionY", errors);
        if (request.Level is < 1)
            errors["level"] = ["Level must be at least 1."];
        return errors;
    }

    public static IReadOnlyDictionary<string, string[]> Validate(TopicStatusInput request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new Dictionary<string, string[]>();
        if (request.StudyStatus is not ("not_started" or "in_progress" or "completed"))
            errors["studyStatus"] = ["Study status must be not_started, in_progress or completed."];
        return errors;
    }

    public static IReadOnlyDictionary<string, string[]> Validate(TopicLinkInput request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new Dictionary<string, string[]>();
        if (request.ParentTopicId == Guid.Empty)
            errors["parentTopicId"] = ["Parent topic ID is required."];
        if (request.ChildTopicId == Guid.Empty)
            errors["childTopicId"] = ["Child topic ID is required."];
        return errors;
    }

    private static void ValidateTitle(string? title, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            errors["title"] = ["Title is required."];
            return;
        }

        var length = 0;
        foreach (var rune in title.EnumerateRunes())
        {
            length++;
            if (length <= 200) continue;
            errors["title"] = ["Title must not exceed 200 characters."];
            return;
        }
    }

    private static void ValidateCoordinate(decimal? coordinate, string field, Dictionary<string, string[]> errors)
    {
        if (coordinate is not { } value) return;
        if (value < -MaxCoordinate || value > MaxCoordinate || decimal.Round(value, 3) != value)
            errors[field] = ["Coordinate is outside the allowed range or has more than three decimal places."];
    }
}
