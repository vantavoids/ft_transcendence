using Auth.Application.Abstractions.OAuth;
using Auth.Domain.AuthUser;
using Auth.Domain.Results;

namespace Auth.UnitTests.Fakes;

internal sealed class FakeOAuthProviderClient : IOAuthProviderClient
{
    private Result<OAuthUserInfo> _exchangeResult =
        new OAuthUserInfo("fake-provider-id", "fake@example.com", true);

    public Uri BuildAuthorizationUrl(string state)
        => new($"https://fake-provider.example.com/oauth/authorize?state={state}");

    public Task<Result<OAuthUserInfo>> ExchangeCodeAsync(
        string code, string state, CancellationToken cancellationToken = default)
        => Task.FromResult(_exchangeResult);

    public void SetupSuccess(OAuthUserInfo info) => _exchangeResult = info;

    public void SetupFailure(Failure failure) => _exchangeResult = failure;
}
