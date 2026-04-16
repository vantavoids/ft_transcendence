using Auth.Application.Abstractions.Messaging;
using Auth.Domain.Results;

namespace Auth.Application.Features.Register;

public sealed record RegisterCommand(
    string Email,
    string Password
) : ICommand<Result<RegisterResult>>;