using System.Net;
using Guild.FunctionalTests.Infrastructure;
using Xunit;

namespace Guild.FunctionalTests.Endpoints;

public sealed class MetricsEndpointTests(GuildApiFactory factory) : IClassFixture<GuildApiFactory>
{
	// /metrics is scraped by Prometheus over the docker network and must not sit
	// behind the JWT wall, mirroring /healthz.
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
		// one completed request so the http.server request-duration histogram exists
		await client.GetAsync("/healthz");

		var body = await (await client.GetAsync("/metrics")).Content.ReadAsStringAsync();

		Assert.Contains("# TYPE", body);
		Assert.Contains("http_server_request_duration_seconds", body);
	}

	[Fact]
	public async Task Metrics_Expose_Domain_Business_Gauges()
	{
		var client = factory.CreateClient();

		var body = await (await client.GetAsync("/metrics")).Content.ReadAsStringAsync();

		// observable gauges fire on scrape, so they surface regardless of collector timing
		Assert.Contains("guild_guilds", body);
		Assert.Contains("guild_members", body);
		Assert.Contains("guild_invites_active", body);
	}
}
