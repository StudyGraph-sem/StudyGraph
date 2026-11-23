namespace StudyGraph.Application.Graph.Validation;

public enum DependencyValidationError
{
    None,
    TopicNotFound,
    RoadmapMismatch,
    SelfDependency,
    DuplicateDependency,
    CycleDetected
}
