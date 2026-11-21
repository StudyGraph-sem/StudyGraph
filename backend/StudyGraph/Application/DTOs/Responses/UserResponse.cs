namespace StudyGraph.Application.DTOs.Responses;

public sealed record UserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    DateTime CreatedAt
);