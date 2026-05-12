using Auth.Application.Abstractions.Messaging;
using Auth.Domain.AuthUser;
using Auth.Domain.Results;

namespace Auth.Application.Features.OAuth;

public sealed record OAuthCallbackCommand(OAuthProvider Provider, string Code)
    : ICommand<Result<OAuthCallbackResult>>;
