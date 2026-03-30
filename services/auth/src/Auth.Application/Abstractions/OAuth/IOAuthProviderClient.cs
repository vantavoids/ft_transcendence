using Auth.Domain.Results;

namespace Auth.Application.Abstractions.OAuth;

public interface IOAuthProviderClient
{
    string BuildAuthorizationUrl(string state);

    Task<Result<string>> GetUserIdAsync(
        string code,
        CancellationToken cancellationToken = default);
}
