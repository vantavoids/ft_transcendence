using Auth.Application.Contracts;
using Auth.Application.Features.DeleteMe;
using Auth.Domain.AuthUser;
using Auth.Domain.Results;
using Auth.UnitTests.Fakes;
using Xunit;

namespace Auth.UnitTests.Application;

public sealed class DeleteMeHandlerTests
{
    private readonly FakeIdGenerator        _idGenerator  = new();
    private readonly FakeAuthUserRepository _repo         = new();
    private readonly FakeGuildClient        _guildClient  = new();
    private readonly FakeEventBus           _eventBus     = new();
    private readonly FakeClock              _clock        = new();

    private Task<Result> Handle(long userId)
    {
        var handler = HandlerFactory.CreateCommand<DeleteMeCommand, Result>(
            _repo, _guildClient, _eventBus, _clock);
        return handler.HandleAsync(new DeleteMeCommand(userId));
    }

    private async Task<AuthUser> SeedEmailUser()
    {
        var user = AuthUser.CreateEmailPasswordUser(
            id: _idGenerator.NextId(),
            email: "user@example.com",
            passwordHash: "hashed:password",
            now: _clock.UtcNow).Value;
        await _repo.AddAsync(user);
        return user;
    }

    private async Task<AuthUser> SeedEmailUserWithRefreshToken()
    {
        var now  = _clock.UtcNow;
        var user = AuthUser.CreateEmailPasswordUser(
            id: _idGenerator.NextId(),
            email: "user@example.com",
            passwordHash: "hashed:password",
            now: now).Value;
        user.SetRefreshToken("some-token-hash", now, now.AddDays(7));
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
    public async Task UserOwnsGuilds_ReturnsOwnedGuildsConflict()
    {
        var user = await SeedEmailUser();
        _guildClient.OwnedGuildsCount = 1;

        var result = await Handle(user.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.OwnedGuildsConflict.Code, result.Error.Code);
    }

    [Fact]
    public async Task ValidUser_ReturnsOk()
    {
        var user   = await SeedEmailUser();
        var result = await Handle(user.Id);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ValidUser_SoftDeletesUser()
    {
        var user = await SeedEmailUser();
        await Handle(user.Id);

        var stored = _repo.Store.Values.Single();
        Assert.True(stored.IsDeleted);
    }

    [Fact]
    public async Task ValidUser_RevokesRefreshToken()
    {
        var user = await SeedEmailUserWithRefreshToken();
        await Handle(user.Id);

        var stored = _repo.Store.Values.Single();
        Assert.True(stored.RefreshToken?.Revoked);
    }

    [Fact]
    public async Task ValidUser_PublishesUserDeleted()
    {
        var user = await SeedEmailUser();
        await Handle(user.Id);

        var evt = Assert.Single(_eventBus.Published.OfType<UserDeleted>());
        Assert.Equal(user.Id, evt.UserId);
    }
}
