using Auth.Application.Features.GetMe;
using Auth.Domain.AuthUser;
using Auth.Domain.Results;
using Auth.UnitTests.Fakes;
using Xunit;

namespace Auth.UnitTests.Application;

public sealed class GetMeHandlerTests
{
    private readonly FakeIdGenerator        _idGenerator = new();
    private readonly FakeAuthUserRepository _repo        = new();
    private readonly FakeClock              _clock       = new();
    private readonly FakeSecretHasher       _hasher      = new();

    private Task<Result<GetMeResponse>> Handle(long userId)
    {
        var handler = HandlerFactory.CreateQuery<GetMeQuery, Result<GetMeResponse>>(_repo);
        return handler.HandleAsync(new GetMeQuery(userId));
    }

    private async Task<AuthUser> SeedEmailUser(string email = "user@example.com", string password = "password123")
    {
        var user = AuthUser.CreateEmailPasswordUser(
            id: _idGenerator.NextId(),
            email: email,
            passwordHash: _hasher.Hash(password),
            now: _clock.UtcNow).Value;
        await _repo.AddAsync(user);
        return user;
    }

    private async Task<AuthUser> SeedOAuthUser(OAuthProvider provider = OAuthProvider.Github, string oauthId = "gh-123")
    {
        var user = AuthUser.CreateOAuthUser(
            id: _idGenerator.NextId(),
            oauthProvider: provider,
            oauthId: oauthId,
            now: _clock.UtcNow).Value;
        await _repo.AddAsync(user);
        return user;
    }

    [Fact]
    public async Task UnknownUser_ReturnsInvalidAccessToken()
    {
        var result = await Handle(999L);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.InvalidAccessToken.Code, result.Error.Code);
    }

    [Fact]
    public async Task SoftDeletedUser_ReturnsInvalidAccessToken()
    {
        var user = await SeedEmailUser();
        user.SoftDelete(_clock.UtcNow);
        _repo.Update(user);

        var result = await Handle(user.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.InvalidAccessToken.Code, result.Error.Code);
    }

    [Fact]
    public async Task EmailUser_ReturnsCorrectResponse()
    {
        var user = await SeedEmailUser("user@example.com");

        var result = await Handle(user.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(user.Id.ToString(), result.Value.Id);
        Assert.Equal("user@example.com", result.Value.Email);
        Assert.False(result.Value.EmailVerified);
        Assert.Empty(result.Value.OAuthProviders);
    }

    [Fact]
    public async Task EmailUser_WithVerifiedEmail_ReturnsEmailVerifiedTrue()
    {
        var user = await SeedEmailUser();
        user.VerifyEmail(_clock.UtcNow);
        _repo.Update(user);

        var result = await Handle(user.Id);

        Assert.True(result.Succeeded);
        Assert.True(result.Value.EmailVerified);
    }

    [Fact]
    public async Task OAuthUser_ReturnsCorrectResponse()
    {
        var user = await SeedOAuthUser(OAuthProvider.Github, "gh-123");

        var result = await Handle(user.Id);

        Assert.True(result.Succeeded);
        Assert.Null(result.Value.Email);
        Assert.False(result.Value.EmailVerified);
        Assert.Equal(["github"], result.Value.OAuthProviders);
    }
}
