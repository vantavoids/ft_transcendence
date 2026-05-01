namespace Auth.Presentation.Contracts.Login;

public sealed record LoginRequest(
    string Email,
    string Password
);
