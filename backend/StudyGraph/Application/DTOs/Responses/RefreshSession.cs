namespace StudyGraph.Application.DTOs.Responses;
public sealed record RefreshSession(string Token, DateTime ExpiresAt, AuthResponse Auth);
