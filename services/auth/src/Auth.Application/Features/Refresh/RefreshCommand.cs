using Auth.Application.Abstractions.Messaging;
using Auth.Domain.Results;

namespace Auth.Application.Features.Refresh;

public sealed record RefreshCommand(
    string RefreshToken
) : ICommand<Result<RefreshResult>>;
