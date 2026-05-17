using Auth.Application.Abstractions.Security;

namespace Auth.UnitTests.Fakes;

internal sealed class FakeTokenGenerator : ITokenGenerator
{
    public string GenerateAccessToken(long userId) => $"access-token-{userId}";

    public string GenerateRefreshToken() => "fake-refresh-token";

    public TimeSpan GetRefreshTokenLifetime() => TimeSpan.FromDays(7);
}
