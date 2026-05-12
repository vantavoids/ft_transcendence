namespace Auth.Application.Features.OAuth;

public sealed record OAuthCallbackResult(string AccessToken, string RefreshToken, bool IsNewUser);
