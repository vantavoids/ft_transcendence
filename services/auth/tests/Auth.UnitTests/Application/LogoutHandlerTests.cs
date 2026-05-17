using Auth.Application.Events;
using Auth.Application.Features.Logout;
using Auth.Domain.AuthUser;
using Auth.Domain.Results;
using Auth.UnitTests.Fakes;
using Xunit;

namespace Auth.UnitTests.Application;

public sealed class LogoutHandlerTests
{
    private readonly FakeIdGenerator        _idGenerator = new();
    private readonly FakeAuthUserRepository _repo        = new();
    private readonly FakeEventBus           _eventBus    = new();
    private readonly FakeClock              _clock       = new();

    private Task<Result> Handle(long userId)
    {
        var handler = HandlerFactory.CreateCommand<LogoutCommand, Result>(
            _repo, _eventBus, _clock);
        return handler.HandleAsync(new LogoutCommand(userId));
    }

    private async Task<AuthUser> SeedUser()
    {
        var user = AuthUser.CreateEmailPasswordUser(
            id: _idGenerator.NextId(),
            email: "user@example.com",
            passwordHash: "hashed:pw",
            now: _clock.UtcNow).Value;

        var now = _clock.UtcNow;
        user.SetRefreshToken("det:some-token", now, now.AddDays(7));

        await _repo.AddAsync(user);
        return user;
    }

    [Fact]
    public async Task ValidUser_ReturnsSuccess()
    {
        var user = await SeedUser();

        var result = await Handle(user.Id);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ValidUser_RevokesRefreshToken()
    {
        var user = await SeedUser();

        await Handle(user.Id);

        var stored = _repo.Store[user.Id];
        Assert.NotNull(stored.RefreshToken);
        Assert.True(stored.RefreshToken.Revoked);
    }

    [Fact]
    public async Task ValidUser_PublishesUserLoggedOutEvent()
    {
        var user = await SeedUser();

        await Handle(user.Id);

        var evt = Assert.Single(_eventBus.Published);
        var loggedOut = Assert.IsType<UserLoggedOutEvent>(evt);
        Assert.Equal(user.Id, loggedOut.UserId);
    }

    [Fact]
    public async Task UnknownUser_ReturnsFailure()
    {
        var result = await Handle(999L);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.InvalidAccessToken.Code, result.Error.Code);
    }

    [Fact]
    public async Task UnknownUser_DoesNotPublishEvent()
    {
        await Handle(999L);

        Assert.Empty(_eventBus.Published);
    }

    [Fact]
    public async Task SoftDeletedUser_ReturnsFailure()
    {
        var user = await SeedUser();
        user.SoftDelete(_clock.UtcNow);
        _repo.Update(user);

        var result = await Handle(user.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.InvalidAccessToken.Code, result.Error.Code);
    }
}
