using Auth.Application.Features.Refresh;
using Auth.Domain.AuthUser;
using Auth.Domain.Results;
using Auth.UnitTests.Fakes;
using Xunit;

namespace Auth.UnitTests.Application;

public sealed class RefreshHandlerTests
{
    private readonly FakeIdGenerator        _idGenerator = new();
    private readonly FakeAuthUserRepository _repo        = new();
    private readonly FakeSecretHasher       _hasher      = new();
    private readonly FakeTokenGenerator     _tokens      = new();
    private readonly FakeClock              _clock       = new();

    private Task<Result<RefreshResult>> Handle(string rawToken)
    {
        var handler = HandlerFactory.CreateCommand<RefreshCommand, Result<RefreshResult>>(
            _repo, _hasher, _tokens, _clock);
        return handler.HandleAsync(new RefreshCommand(rawToken));
    }

    private async Task<AuthUser> SeedUserWithToken(
        string rawToken,
        DateTimeOffset? issuedAt  = null,
        DateTimeOffset? expiresAt = null,
        bool revoked = false)
    {
        var now  = _clock.UtcNow;
        var user = AuthUser.CreateEmailPasswordUser(
            id: _idGenerator.NextId(),
            email: "user@example.com",
            passwordHash: "hashed:pw",
            now: now).Value;

        user.SetRefreshToken(
            _hasher.HashDeterministic(rawToken),
            issuedAt  ?? now,
            expiresAt ?? now.AddDays(7));

        if (revoked)
            user.RevokeRefreshToken(now);

        await _repo.AddAsync(user);
        return user;
    }

    [Fact]
    public async Task ValidToken_ReturnsNewTokens()
    {
        await SeedUserWithToken("my-token");

        var result = await Handle("my-token");

        Assert.True(result.Succeeded);
        Assert.NotEmpty(result.Value.AccessToken);
        Assert.NotEmpty(result.Value.RefreshToken);
    }

    [Fact]
    public async Task ValidToken_RotatesRefreshToken()
    {
        await SeedUserWithToken("my-token");

        await Handle("my-token");

        var user = _repo.Store.Values.Single();
        Assert.NotNull(user.RefreshToken);
        Assert.NotEqual("det:my-token", user.RefreshToken.Hash);
        Assert.True(user.RefreshToken.IsActive(_clock.UtcNow));
    }

    [Fact]
    public async Task ValidToken_AccessTokenContainsUserId()
    {
        await SeedUserWithToken("my-token");

        var result = await Handle("my-token");

        var userId = _repo.Store.Values.Single().Id;
        Assert.Equal($"access-token-{userId}", result.Value.AccessToken);
    }

    [Fact]
    public async Task UnknownToken_ReturnsInvalidRefreshToken()
    {
        await SeedUserWithToken("real-token");

        var result = await Handle("wrong-token");

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.InvalidRefreshToken.Code, result.Error.Code);
    }

    [Fact]
    public async Task ExpiredToken_ReturnsInvalidRefreshToken()
    {
        var past = _clock.UtcNow.AddDays(-8);
        await SeedUserWithToken("my-token", issuedAt: past, expiresAt: past.AddDays(1));

        var result = await Handle("my-token");

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.InvalidRefreshToken.Code, result.Error.Code);
    }

    [Fact]
    public async Task RevokedToken_ReturnsInvalidRefreshToken()
    {
        await SeedUserWithToken("my-token", revoked: true);

        var result = await Handle("my-token");

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.InvalidRefreshToken.Code, result.Error.Code);
    }

    [Fact]
    public async Task SoftDeletedUser_ReturnsInvalidRefreshToken()
    {
        var user = await SeedUserWithToken("my-token");
        user.SoftDelete(_clock.UtcNow);
        _repo.Update(user);

        var result = await Handle("my-token");

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.InvalidRefreshToken.Code, result.Error.Code);
    }
}
