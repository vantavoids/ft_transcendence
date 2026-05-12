using Auth.Application.Abstractions.Messaging;
using Auth.Domain.Results;

namespace Auth.Application.Features.OAuth;

internal sealed class OAuthCallbackHandler
    : ICommandHandler<OAuthCallbackCommand, Result<OAuthCallbackResult>>
{
    public Task<Result<OAuthCallbackResult>> HandleAsync(
        OAuthCallbackCommand command, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
