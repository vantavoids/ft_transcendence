using Auth.Application.Abstractions.Messaging;
using Auth.Application.Events;
using Auth.Application.Features.OAuth;
using Auth.Domain.AuthUser;
using Auth.Domain.Results;
using Auth.UnitTests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Auth.UnitTests.Application;

public sealed class OAuthCallbackHandlerTests
{
    private static (IServiceProvider sp, FakeOAuthProviderClient client) BuildServiceProvider()
    {
        var client = new FakeOAuthProviderClient();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<Auth.Application.Abstractions.OAuth.IOAuthProviderClient>(
            OAuthProvider.FortyTwo, client);
        return (services.BuildServiceProvider(), client);
    }

    private static ICommandHandler<OAuthCallbackCommand, Result<OAuthCallbackResult>> BuildHandler(
        IServiceProvider sp,
        FakeAuthUserRepository? repo = null,
        FakeEventBus? eventBus = null)
    {
        return HandlerFactory.CreateCommand<OAuthCallbackCommand, Result<OAuthCallbackResult>>(
            sp,
            repo ?? new FakeAuthUserRepository(),
            new FakeTokenGenerator(),
            new FakeSecretHasher(),
            new FakeIdGenerator(),
            new FakeClock(),
            eventBus ?? new FakeEventBus());
    }

    [Fact]
    public async Task NewUser_CreatesUserAndPublishesRegisteredEvent()
    {
        var (sp, _) = BuildServiceProvider();
        var repo = new FakeAuthUserRepository();
        var eventBus = new FakeEventBus();
        var handler = BuildHandler(sp, repo, eventBus);

        var result = await handler.HandleAsync(
            new OAuthCallbackCommand(OAuthProvider.FortyTwo, "code-xyz", "state-abc"));

        Assert.True(result.Succeeded);
        Assert.True(result.Value.IsNewUser);
        Assert.Single(repo.Store);
        Assert.Single(eventBus.Published);
        Assert.IsType<UserRegisteredEvent>(eventBus.Published[0]);
    }

    [Fact]
    public async Task ExistingUser_ReturnsTokensAndDoesNotPublishEvent()
    {
        var (sp, _) = BuildServiceProvider();
        var repo = new FakeAuthUserRepository();
        var eventBus = new FakeEventBus();
        var idGen = new FakeIdGenerator();

        var existingUser = AuthUser.CreateOAuthUser(
            idGen.NextId(),
            OAuthProvider.FortyTwo,
            "fake-provider-id",
            DateTimeOffset.UtcNow).Value;
        await repo.AddAsync(existingUser);

        var handler = BuildHandler(sp, repo, eventBus);

        var result = await handler.HandleAsync(
            new OAuthCallbackCommand(OAuthProvider.FortyTwo, "code-xyz", "state-abc"));

        Assert.True(result.Succeeded);
        Assert.False(result.Value.IsNewUser);
        Assert.Single(repo.Store);
        Assert.Empty(eventBus.Published);
    }

    [Fact]
    public async Task ProviderError_ReturnsFailure()
    {
        var (sp, client) = BuildServiceProvider();
        client.SetupFailure(AuthFailures.OAuthProviderError);
        var handler = BuildHandler(sp);

        var result = await handler.HandleAsync(
            new OAuthCallbackCommand(OAuthProvider.FortyTwo, "bad-code", "state"));

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.OAuthProviderError.Code, result.Error.Code);
    }

    [Fact]
    public async Task UnknownProvider_ReturnsFailure()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var handler = BuildHandler(sp);

        var result = await handler.HandleAsync(
            new OAuthCallbackCommand(OAuthProvider.Unknown, "code", "state"));

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.InvalidOAuthProvider.Code, result.Error.Code);
    }

    [Fact]
    public async Task AccessToken_ContainsUserId()
    {
        var (sp, _) = BuildServiceProvider();
        var repo = new FakeAuthUserRepository();
        var handler = BuildHandler(sp, repo);

        var result = await handler.HandleAsync(
            new OAuthCallbackCommand(OAuthProvider.FortyTwo, "code", "state"));

        Assert.True(result.Succeeded);
        var userId = repo.Store.Keys.Single();
        Assert.Equal($"access-token-{userId}", result.Value.AccessToken);
    }
}
