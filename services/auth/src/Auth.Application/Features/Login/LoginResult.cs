namespace Auth.Application.Features.Login;

public sealed record LoginResult(
    long UserId,
    string AccessToken,
    string RefreshToken
);
