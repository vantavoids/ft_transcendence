namespace Auth.Application.Features.OAuth;

public sealed record OAuthLoginResult(Uri RedirectUri, string State);
