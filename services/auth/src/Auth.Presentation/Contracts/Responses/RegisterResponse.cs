namespace Auth.Presentation.Contracts.Responses;

public sealed record RegisterResponse(
    long UserId,
    string AccessToken
);
