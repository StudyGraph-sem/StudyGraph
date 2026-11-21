namespace StudyGraph.Application.DTOs.Responses;

public sealed record AuthResponse(
    string AccessToken,
    DateTime ExpiresAt,
    UserResponse User
);
