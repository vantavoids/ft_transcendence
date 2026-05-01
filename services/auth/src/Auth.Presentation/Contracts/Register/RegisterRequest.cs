namespace Auth.Presentation.Contracts.Register;

public sealed record RegisterRequest(
    string Email,
    string Password
);