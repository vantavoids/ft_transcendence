namespace Auth.Presentation.Contracts.Responses;

public sealed record LoginResponse(
    long UserId,
    string AccessToken
);
