using System.Text.Json.Serialization;

namespace Auth.Application.Features.GetMe;

// JsonNamingPolicy.SnakeCaseLower convert OAuthProviders into o_auth_providers
// Specified JsonPropertyName to respect the contract
public sealed record GetMeResponse(
    string Id,
    string? Email,
    bool EmailVerified,
    [property: JsonPropertyName("oauth_providers")] string[] OAuthProviders,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
