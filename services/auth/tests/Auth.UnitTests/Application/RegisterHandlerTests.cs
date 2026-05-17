using Auth.Application.Events;
using Auth.Application.Features.Register;
using Auth.Domain.Results;
using Auth.UnitTests.Fakes;
using Xunit;

namespace Auth.UnitTests.Application;

public sealed class RegisterHandlerTests
{
    private readonly FakeIdGenerator        _idGenerator = new();
    private readonly FakeAuthUserRepository _repo        = new();
    private readonly FakeTokenGenerator     _tokens      = new();
    private readonly FakeSecretHasher       _hasher      = new();
    private readonly FakeEventBus           _eventBus    = new();
    private readonly FakeClock              _clock       = new();

    private Task<Result<RegisterResult>> Handle(string email, string password)
    {
        var handler = HandlerFactory.CreateCommand<RegisterCommand, Result<RegisterResult>>(
            _idGenerator, _repo, _tokens, _hasher, _eventBus, _clock);
        return handler.HandleAsync(new RegisterCommand(email, password));
    }

    [Fact]
    public async Task ValidRegistration_ReturnsSuccessWithTokens()
    {
        var result = await Handle("user@example.com", "password123");

        Assert.True(result.Succeeded);
        Assert.Equal($"access-token-{1_000_000_000_000_000_001L}", result.Value.AccessToken);
        Assert.Equal("fake-refresh-token", result.Value.RefreshToken);
        Assert.Equal(1_000_000_000_000_000_001L, result.Value.UserId);
    }

    [Fact]
    public async Task ValidRegistration_StoresUserWithHashedPassword()
    {
        await Handle("user@example.com", "password123");

        var user = Assert.Single(_repo.Store.Values);
        Assert.Equal("user@example.com", user.Email!.Value);
        Assert.Equal("hashed:password123", user.PasswordHash);
    }

    [Fact]
    public async Task ValidRegistration_StoresDeterministicHashedRefreshToken()
    {
        await Handle("user@example.com", "password123");

        var user = Assert.Single(_repo.Store.Values);
        Assert.NotNull(user.RefreshToken);
        Assert.Equal("det:fake-refresh-token", user.RefreshToken.Hash);
        Assert.True(user.RefreshToken.IsActive(_clock.UtcNow));
    }

    [Fact]
    public async Task ValidRegistration_PublishesUserRegisteredEvent()
    {
        await Handle("user@example.com", "password123");

        var evt = Assert.Single(_eventBus.Published);
        var registered = Assert.IsType<UserRegisteredEvent>(evt);
        Assert.Equal(1_000_000_000_000_000_001L, registered.UserId);
        Assert.Equal("user@example.com", registered.Email);
    }

    [Fact]
    public async Task DuplicateEmail_ReturnsEmailAlreadyRegistered()
    {
        await Handle("user@example.com", "password123");

        var result = await Handle("user@example.com", "other-password");

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.EmailAlreadyRegistered.Code, result.Error.Code);
    }

    [Fact]
    public async Task DuplicateEmail_DoesNotPublishEvent()
    {
        await Handle("user@example.com", "password123");
        await Handle("user@example.com", "other-password");

        Assert.Single(_eventBus.Published);
    }

    [Fact]
    public async Task InvalidEmail_ReturnsInvalidEmail()
    {
        var result = await Handle("not-an-email", "password123");

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.InvalidEmail.Code, result.Error.Code);
    }
}
