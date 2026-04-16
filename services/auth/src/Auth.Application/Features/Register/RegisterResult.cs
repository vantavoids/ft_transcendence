namespace Auth.Application.Features.Register;

public sealed record RegisterResult(
    long UserId,
    string AccessToken,
    string RefreshToken
);
