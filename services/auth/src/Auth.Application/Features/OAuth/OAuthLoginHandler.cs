using Auth.Application.Abstractions.Messaging;
using Auth.Domain.Results;

namespace Auth.Application.Features.OAuth;

internal sealed class OAuthLoginHandler
    : ICommandHandler<OAuthLoginCommand, Result<OAuthLoginResult>>
{
    public Task<Result<OAuthLoginResult>> HandleAsync(
        OAuthLoginCommand command, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
