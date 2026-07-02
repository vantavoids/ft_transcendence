using System.Net;
using Auth.FunctionalTests.Infrastructure;
using Xunit;

namespace Auth.FunctionalTests.Endpoints;

public sealed class MetricsEndpointTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    // /metrics is scraped by Prometheus over the docker network and must not sit
    // behind auth, mirroring /healthz.
    [Fact]
    public async Task Metrics_Is_Anonymous_And_PrometheusText()
    {
        var client = factory.CreateClient();

        var resp = await client.GetAsync("/metrics");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("text/plain", resp.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Metrics_Expose_HttpServer_RedMetric_AfterTraffic()
    {
        var client = factory.CreateClient();
        await client.GetAsync("/does-not-exist"); // any completed request populates the histogram

        string body = "";
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(3);
        do
        {
            body = await (await client.GetAsync("/metrics")).Content.ReadAsStringAsync();
            if (body.Contains("http_server_request_duration_seconds", StringComparison.Ordinal))
                break;
            await Task.Delay(50);
        }
        while (DateTime.UtcNow < deadline);

        Assert.Contains("# TYPE", body);
        Assert.Contains("http_server_request_duration_seconds", body);
    }

    // existence only: exact gauge values bleed across the parallel multi-host OTel
    // listener, so value assertions would flake. the counts are verified at runtime.
    [Fact]
    public async Task Metrics_Expose_Auth_Domain_Gauges()
    {
        var client = factory.CreateClient();

        var body = await (await client.GetAsync("/metrics")).Content.ReadAsStringAsync();

        Assert.Contains("auth_accounts", body);
        Assert.Contains("auth_accounts_oauth", body);
        Assert.Contains("auth_sessions_active", body);
    }
}
