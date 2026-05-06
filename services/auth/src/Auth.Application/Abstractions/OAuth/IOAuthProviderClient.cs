using Auth.Domain.Results;
using Auth.Domain.AuthUser;

namespace Auth.Application.Abstractions.OAuth;

public interface IOAuthProviderClient
{
    Uri                         BuildAuthorizationUrl(string state);
    Task<Result<OAuthUserInfo>> ExchangeCodeAsync(string code,
                                    CancellationToken cancellationToken = default);
}
