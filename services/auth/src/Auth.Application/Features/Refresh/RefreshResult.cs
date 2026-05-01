namespace Auth.Application.Features.Refresh;

public sealed record RefreshResult(
    string AccessToken,
    string RefreshToken
);
