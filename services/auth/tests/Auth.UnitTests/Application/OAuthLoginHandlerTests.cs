using Auth.Application.Features.OAuth;
using Auth.Domain.AuthUser;
using Auth.Domain.Results;
using Auth.UnitTests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Auth.UnitTests.Application;

public sealed class OAuthLoginHandlerTests
{
    private static IServiceProvider BuildServiceProvider(
        OAuthProvider provider, FakeOAuthProviderClient client)
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<Auth.Application.Abstractions.OAuth.IOAuthProviderClient>(provider, client);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task ValidProvider_ReturnsRedirectUriAndState()
    {
        var fakeClient = new FakeOAuthProviderClient();
        var sp = BuildServiceProvider(OAuthProvider.FortyTwo, fakeClient);
        var handler = HandlerFactory.CreateCommand<OAuthLoginCommand, Result<OAuthLoginResult>>(sp);

        var result = await handler.HandleAsync(new OAuthLoginCommand(OAuthProvider.FortyTwo));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value.RedirectUri);
        Assert.False(string.IsNullOrEmpty(result.Value.State));
    }

    [Fact]
    public async Task UnknownProvider_ReturnsFailure()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var handler = HandlerFactory.CreateCommand<OAuthLoginCommand, Result<OAuthLoginResult>>(sp);

        var result = await handler.HandleAsync(new OAuthLoginCommand(OAuthProvider.Unknown));

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.InvalidOAuthProvider.Code, result.Error.Code);
    }

    [Fact]
    public async Task UnregisteredProvider_ReturnsFailure()
    {
        // ? Valid provider but not registered in DI
        var sp = new ServiceCollection().BuildServiceProvider();
        var handler = HandlerFactory.CreateCommand<OAuthLoginCommand, Result<OAuthLoginResult>>(sp);

        var result = await handler.HandleAsync(new OAuthLoginCommand(OAuthProvider.Github));

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.InvalidOAuthProvider.Code, result.Error.Code);
    }

    [Fact]
    public async Task TwoConsecutiveCalls_ProduceDifferentStates()
    {
        var fakeClient = new FakeOAuthProviderClient();
        var sp = BuildServiceProvider(OAuthProvider.Google, fakeClient);
        var handler = HandlerFactory.CreateCommand<OAuthLoginCommand, Result<OAuthLoginResult>>(sp);

        var r1 = await handler.HandleAsync(new OAuthLoginCommand(OAuthProvider.Google));
        var r2 = await handler.HandleAsync(new OAuthLoginCommand(OAuthProvider.Google));

        Assert.True(r1.Succeeded);
        Assert.True(r2.Succeeded);
        Assert.NotEqual(r1.Value.State, r2.Value.State);
    }
}
