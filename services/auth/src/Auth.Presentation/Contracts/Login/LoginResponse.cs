namespace Auth.Presentation.Contracts.Login;

public sealed record LoginResponse(
    string UserId,
    string AccessToken
);
