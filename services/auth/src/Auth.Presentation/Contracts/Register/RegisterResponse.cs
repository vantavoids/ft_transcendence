namespace Auth.Presentation.Contracts.Register;

public sealed record RegisterResponse(
    string UserId,
    string AccessToken
);
