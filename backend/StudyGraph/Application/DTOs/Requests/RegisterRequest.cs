namespace StudyGraph.Application.DTOs.Requests;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string DisplayName
);