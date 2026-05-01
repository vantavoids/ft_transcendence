namespace Auth.Presentation.Contracts.Register;

public sealed record RegisterResponse(
    long UserId,
    string AccessToken
);
