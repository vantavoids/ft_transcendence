using System.Net;
using Chat.FunctionalTests.Infrastructure;
using Xunit;

namespace Chat.FunctionalTests.Features;

public sealed class MetricsEndpointTests(ChatApiFactory factory) : IClassFixture<ChatApiFactory>
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
		// any completed request populates the http.server histogram; a 404 avoids
		// /healthz (which probes Scylla and throws in the test host)
		await client.GetAsync("/does-not-exist");

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

	// gauge existence is robust to the parallel multi-host OTel bleed; the count
	// logic behind them is unit-tested deterministically in CallRegistryTests.
	[Fact]
	public async Task Metrics_Expose_Chat_Domain_Gauges()
	{
		var client = factory.CreateClient();

		var body = await (await client.GetAsync("/metrics")).Content.ReadAsStringAsync();

		Assert.Contains("chat_hub_connected_users", body);
		Assert.Contains("chat_signaling_connected_users", body);
		Assert.Contains("chat_calls_active", body);
		Assert.Contains("chat_calls_ringing", body);
	}
}
