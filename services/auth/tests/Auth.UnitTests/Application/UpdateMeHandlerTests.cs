using Auth.Application.Features.GetMe;
using Auth.Application.Features.UpdateMe;
using Auth.Domain.AuthUser;
using Auth.Domain.Results;
using Auth.UnitTests.Fakes;
using Xunit;

namespace Auth.UnitTests.Application;

public sealed class UpdateMeHandlerTests
{
    private readonly FakeIdGenerator        _idGenerator = new();
    private readonly FakeAuthUserRepository _repo        = new();
    private readonly FakeSecretHasher       _hasher      = new();
    private readonly FakeClock              _clock       = new();

    private Task<Result<GetMeResponse>> Handle(
        long userId,
        string? email           = null,
        string? currentPassword = null,
        string? newPassword     = null)
    {
        var handler = HandlerFactory.CreateCommand<UpdateMeCommand, Result<GetMeResponse>>(
            _repo, _hasher, _clock);
        return handler.HandleAsync(new UpdateMeCommand(userId, email, currentPassword, newPassword));
    }

    private async Task<AuthUser> SeedEmailUser(
        string email    = "user@example.com",
        string password = "password123")
    {
        var user = AuthUser.CreateEmailPasswordUser(
            id: _idGenerator.NextId(),
            email: email,
            passwordHash: _hasher.Hash(password),
            now: _clock.UtcNow).Value;
        await _repo.AddAsync(user);
        return user;
    }

    private async Task<AuthUser> SeedEmailUserWithRefreshToken(
        string email    = "user@example.com",
        string password = "password123")
    {
        var now  = _clock.UtcNow;
        var user = AuthUser.CreateEmailPasswordUser(
            id: _idGenerator.NextId(),
            email: email,
            passwordHash: _hasher.Hash(password),
            now: now).Value;
        user.SetRefreshToken("some-token-hash", now, now.AddDays(7));
        await _repo.AddAsync(user);
        return user;
    }

    [Fact]
    public async Task NoFieldsProvided_ReturnsAtLeastOneFieldRequired()
    {
        var user   = await SeedEmailUser();
        var result = await Handle(user.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.AtLeastOneFieldToPatch.Code, result.Error.Code);
    }

    [Fact]
    public async Task WhitespaceNewPassword_ReturnsWeakPassword()
    {
        var user   = await SeedEmailUser();
        var result = await Handle(user.Id, newPassword: "   ");

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.WeakPassword.Code, result.Error.Code);
    }

    [Fact]
    public async Task UnknownUser_ReturnsInvalidAccessToken()
    {
        var result = await Handle(999L, email: "new@example.com", currentPassword: "pw");

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.InvalidAccessToken.Code, result.Error.Code);
    }

    [Fact]
    public async Task SoftDeletedUser_ReturnsInvalidAccessToken()
    {
        var user = await SeedEmailUser();
        user.SoftDelete(_clock.UtcNow);
        _repo.Update(user);

        var result = await Handle(user.Id, email: "new@example.com", currentPassword: "password123");

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.InvalidAccessToken.Code, result.Error.Code);
    }

    [Fact]
    public async Task OAuthUser_ReturnsOAuthCantPatchEmail()
    {
        var user = AuthUser.CreateOAuthUser(
            id: _idGenerator.NextId(),
            oauthProvider: OAuthProvider.Github,
            oauthId: "gh-123",
            now: _clock.UtcNow).Value;
        await _repo.AddAsync(user);

        var result = await Handle(user.Id, email: "new@example.com", currentPassword: "anything");

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.OAuthCantPatchEmail.Code, result.Error.Code);
    }

    [Fact]
    public async Task MissingCurrentPassword_ReturnsInvalidCredentials()
    {
        var user   = await SeedEmailUser();
        var result = await Handle(user.Id, email: "new@example.com", currentPassword: null);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.InvalidCredentials.Code, result.Error.Code);
    }

    [Fact]
    public async Task WrongCurrentPassword_ReturnsInvalidCredentials()
    {
        var user   = await SeedEmailUser(password: "correct-password");
        var result = await Handle(user.Id, email: "new@example.com", currentPassword: "wrong-password");

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.InvalidCredentials.Code, result.Error.Code);
    }

    [Fact]
    public async Task EmailAlreadyTaken_ReturnsEmailAlreadyRegistered()
    {
        await SeedEmailUser("taken@example.com", "pass1");
        var changer = await SeedEmailUser("changer@example.com", "pass2");

        var result = await Handle(changer.Id, email: "taken@example.com", currentPassword: "pass2");

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.EmailAlreadyRegistered.Code, result.Error.Code);
    }

    [Fact]
    public async Task ValidEmailChange_ReturnsUpdatedEmail()
    {
        var user   = await SeedEmailUser("old@example.com", "password123");
        var result = await Handle(user.Id, email: "new@example.com", currentPassword: "password123");

        Assert.True(result.Succeeded);
        Assert.Equal("new@example.com", result.Value.Email);
    }

    [Fact]
    public async Task EmailChange_SetsEmailVerifiedToFalse()
    {
        var user = await SeedEmailUser("old@example.com", "password123");
        user.VerifyEmail(_clock.UtcNow);
        _repo.Update(user);

        var result = await Handle(user.Id, email: "new@example.com", currentPassword: "password123");

        Assert.True(result.Succeeded);
        Assert.False(result.Value.EmailVerified);
    }

    [Fact]
    public async Task PasswordChange_UpdatesPasswordHash()
    {
        var user   = await SeedEmailUser(password: "old-password");
        var result = await Handle(user.Id, currentPassword: "old-password", newPassword: "new-password");

        Assert.True(result.Succeeded);
        var stored = _repo.Store.Values.Single();
        Assert.Equal(_hasher.Hash("new-password"), stored.PasswordHash);
    }

    [Fact]
    public async Task PasswordChange_RevokesRefreshToken()
    {
        var user   = await SeedEmailUserWithRefreshToken(password: "old-password");
        var result = await Handle(user.Id, currentPassword: "old-password", newPassword: "new-password");

        Assert.True(result.Succeeded);
        var stored = _repo.Store.Values.Single();
        Assert.True(stored.RefreshToken?.Revoked);
    }
}
