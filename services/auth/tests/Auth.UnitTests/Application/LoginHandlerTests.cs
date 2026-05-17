using Auth.Application.Features.Login;
using Auth.Domain.AuthUser;
using Auth.Domain.Results;
using Auth.UnitTests.Fakes;
using Xunit;

namespace Auth.UnitTests.Application;

public sealed class LoginHandlerTests
{
    private readonly FakeIdGenerator        _idGenerator = new();
    private readonly FakeAuthUserRepository _repo        = new();
    private readonly FakeSecretHasher       _hasher      = new();
    private readonly FakeTokenGenerator     _tokens      = new();
    private readonly FakeClock              _clock       = new();

    private Task<Result<LoginResult>> Handle(string email, string password)
    {
        var handler = HandlerFactory.CreateCommand<LoginCommand, Result<LoginResult>>(
            _repo, _hasher, _tokens, _clock);
        return handler.HandleAsync(new LoginCommand(email, password));
    }

    private async Task SeedEmailUser(string email, string password)
    {
        var user = AuthUser.CreateEmailPasswordUser(
            id: _idGenerator.NextId(),
            email: email,
            passwordHash: _hasher.Hash(password),
            _clock.UtcNow).Value;
        await _repo.AddAsync(user);
    }

    [Fact]
    public async Task ValidCredentials_ReturnsSuccessWithTokens()
    {
        await SeedEmailUser("user@example.com", "password123");

        var result = await Handle("user@example.com", "password123");

        Assert.True(result.Succeeded);
        Assert.NotEmpty(result.Value.AccessToken);
        Assert.NotEmpty(result.Value.RefreshToken);
        Assert.Equal("fake-refresh-token", result.Value.RefreshToken);
    }

    [Fact]
    public async Task ValidCredentials_RotatesRefreshToken()
    {
        await SeedEmailUser("user@example.com", "password123");

        await Handle("user@example.com", "password123");

        var user = _repo.Store.Values.Single();
        Assert.NotNull(user.RefreshToken);
        Assert.Equal("det:fake-refresh-token", user.RefreshToken.Hash);
        Assert.True(user.RefreshToken.IsActive(_clock.UtcNow));
    }

    [Fact]
    public async Task AccessToken_ContainsUserId()
    {
        await SeedEmailUser("user@example.com", "password123");

        var result = await Handle("user@example.com", "password123");

        var userId = _repo.Store.Values.Single().Id;
        Assert.Equal($"access-token-{userId}", result.Value.AccessToken);
    }

    [Fact]
    public async Task UnknownEmail_ReturnsInvalidCredentials()
    {
        var result = await Handle("nobody@example.com", "password123");

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.InvalidCredentials.Code, result.Error.Code);
    }

    [Fact]
    public async Task WrongPassword_ReturnsInvalidCredentials()
    {
        await SeedEmailUser("user@example.com", "correct-password");

        var result = await Handle("user@example.com", "wrong-password");

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.InvalidCredentials.Code, result.Error.Code);
    }

    [Fact]
    public async Task SoftDeletedUser_ReturnsInvalidCredentials()
    {
        await SeedEmailUser("user@example.com", "password123");
        var user = _repo.Store.Values.Single();
        user.SoftDelete(_clock.UtcNow);
        _repo.Update(user);

        var result = await Handle("user@example.com", "password123");

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.InvalidCredentials.Code, result.Error.Code);
    }

    [Fact]
    public async Task OAuthOnlyUser_ReturnsInvalidCredentials()
    {
        var oauthUser = AuthUser.CreateOAuthUser(
            id: _idGenerator.NextId(),
            oauthProvider: OAuthProvider.Github,
            oauthId: "gh-123",
            _clock.UtcNow).Value;
        await _repo.AddAsync(oauthUser);

        var result = await Handle("user@example.com", "password123");

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.InvalidCredentials.Code, result.Error.Code);
    }
}
