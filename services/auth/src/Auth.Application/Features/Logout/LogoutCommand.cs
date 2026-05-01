using Auth.Application.Abstractions.Messaging;
using Auth.Domain.Results;

namespace Auth.Application.Features.Logout;

public sealed record LogoutCommand(long UserId) : ICommand<Result>;
