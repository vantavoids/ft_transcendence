namespace Auth.Application.Features.GetMe;

public sealed record GetMeResponse(
    string Id,
    string? Email,
    bool EmailVerified,
    string[] OAuthProviders,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
