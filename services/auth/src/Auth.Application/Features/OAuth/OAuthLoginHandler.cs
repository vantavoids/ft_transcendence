using System.Security.Cryptography;
using Auth.Application.Abstractions.Messaging;
using Auth.Application.Abstractions.OAuth;
using Auth.Domain.AuthUser;
using Auth.Domain.Results;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Application.Features.OAuth;

internal sealed class OAuthLoginHandler(IServiceProvider sp)
    : ICommandHandler<OAuthLoginCommand, Result<OAuthLoginResult>>
{
    public Task<Result<OAuthLoginResult>> HandleAsync(
        OAuthLoginCommand command, CancellationToken cancellationToken = default)
    {
        var client = sp.GetKeyedService<IOAuthProviderClient>(command.Provider);
        if (client is null)
            return Task.FromResult<Result<OAuthLoginResult>>(AuthFailures.InvalidOAuthProvider);

        var state = GenerateState();
        return Task.FromResult<Result<OAuthLoginResult>>(
            new OAuthLoginResult(client.BuildAuthorizationUrl(state), state)
        );
    }

    private static string GenerateState()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
