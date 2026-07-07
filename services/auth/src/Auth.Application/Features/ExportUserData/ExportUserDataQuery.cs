using System.Text.Json.Serialization;
using Auth.Application.Abstractions.Messaging;
using Auth.Domain.Results;

namespace Auth.Application.Features.ExportUserData;

public sealed record ExportUserDataQuery(long UserId) : IQuery<Result<UserDataExportResponse>>;

public sealed record UserDataExportResponse(
	string UserId,
	string? Email,
	bool? EmailVerified,
	[property: JsonPropertyName("oauth_provider")] string? OAuthProvider,
	[property: JsonPropertyName("oauth_id")] string? OAuthId,
	DateTimeOffset? CreatedAt);
