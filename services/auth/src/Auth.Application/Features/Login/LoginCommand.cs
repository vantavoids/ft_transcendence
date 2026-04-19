using Auth.Application.Abstractions.Messaging;
using Auth.Domain.Results;

namespace Auth.Application.Features.Login;

public sealed record LoginCommand(
    string Email,
    string Password
) : ICommand<Result<LoginResult>>;
