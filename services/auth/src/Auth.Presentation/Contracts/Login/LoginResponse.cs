namespace Auth.Presentation.Contracts.Login;

public sealed record LoginResponse(
    long UserId,
    string AccessToken
);
