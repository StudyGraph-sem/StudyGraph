namespace StudyGraph.Application.Graph.Validation;

public sealed class DependencyValidationResult
{
    private DependencyValidationResult(DependencyValidationError error, Guid? missingTopicId)
    {
        Error = error;
        MissingTopicId = missingTopicId;
    }

    public bool IsValid => Error == DependencyValidationError.None;
    public DependencyValidationError Error { get; }
    public Guid? MissingTopicId { get; }

    public static DependencyValidationResult Success()
    {
        return new DependencyValidationResult(DependencyValidationError.None, null);
    }

    public static DependencyValidationResult Failure(
        DependencyValidationError error,
        Guid? missingTopicId = null)
    {
        if (!Enum.IsDefined(error) || error == DependencyValidationError.None)
        {
            throw new ArgumentOutOfRangeException(nameof(error), error, "A failure requires a defined error.");
        }

        if (error == DependencyValidationError.TopicNotFound && missingTopicId is null)
        {
            throw new ArgumentException("The missing topic ID is required.", nameof(missingTopicId));
        }

        if (error != DependencyValidationError.TopicNotFound && missingTopicId is not null)
        {
            throw new ArgumentException("A missing topic ID is only valid for TopicNotFound.", nameof(missingTopicId));
        }

        return new DependencyValidationResult(error, missingTopicId);
    }
}
