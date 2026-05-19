using System.Net;
using Auth.Domain.AuthUser;
using Auth.Domain.Results;
using Auth.Infrastructure.OAuth;
using Xunit;

namespace Auth.UnitTests.Infrastructure;

// Minimal concrete subclass that exposes Send<T> for testing.
sealed class TestableOAuthProvider(HttpClient http) : OAuthProviderBase(http)
{
    public override Uri BuildAuthorizationUrl(string state) => new("https://test.example.com");

    public override Task<Result<OAuthUserInfo>> ExchangeCodeAsync(
        string code, string state, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<Result<T>> CallSend<T>(HttpRequestMessage request, CancellationToken ct = default)
        => Send<T>(request, ct);
}

public sealed class OAuthProviderBaseTests
{
    private sealed record Payload(string Name);

    private static TestableOAuthProvider BuildProvider(HttpStatusCode status, string? body = null)
    {
        var response = new HttpResponseMessage(status);
        if (body is not null)
            response.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        return new TestableOAuthProvider(new HttpClient(new FakeHttpMessageHandler(response)));
    }

    [Fact]
    public async Task Provider4xxResponse_ReturnsOAuthProviderError()
    {
        var provider = BuildProvider(HttpStatusCode.BadRequest);
        using var req = new HttpRequestMessage(HttpMethod.Get, "https://p.test/api");

        var result = await provider.CallSend<Payload>(req);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.OAuthProviderError.Code, result.Error.Code);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(502)]
    [InlineData(503)]
    [InlineData(504)]
    public async Task Provider5xxResponse_ReturnsOAuthUpstreamError(int statusCode)
    {
        var provider = BuildProvider((HttpStatusCode)statusCode);
        using var req = new HttpRequestMessage(HttpMethod.Get, "https://p.test/api");

        var result = await provider.CallSend<Payload>(req);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.OAuthUpstreamError.Code, result.Error.Code);
    }

    [Fact]
    public async Task MalformedJsonResponse_ReturnsOAuthUpstreamError()
    {
        var provider = BuildProvider(HttpStatusCode.OK, "not json {{{{");
        using var req = new HttpRequestMessage(HttpMethod.Get, "https://p.test/api");

        var result = await provider.CallSend<Payload>(req);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.OAuthUpstreamError.Code, result.Error.Code);
    }

    [Fact]
    public async Task NullJsonResponse_ReturnsOAuthUpstreamError()
    {
        var provider = BuildProvider(HttpStatusCode.OK, "null");
        using var req = new HttpRequestMessage(HttpMethod.Get, "https://p.test/api");

        var result = await provider.CallSend<Payload>(req);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthFailures.OAuthUpstreamError.Code, result.Error.Code);
    }

    [Fact]
    public async Task ValidJsonResponse_ReturnsDeserializedData()
    {
        var provider = BuildProvider(HttpStatusCode.OK, "{\"name\":\"hello\"}");
        using var req = new HttpRequestMessage(HttpMethod.Get, "https://p.test/api");

        var result = await provider.CallSend<Payload>(req);

        Assert.True(result.Succeeded);
        Assert.Equal("hello", result.Value.Name);
    }
}
