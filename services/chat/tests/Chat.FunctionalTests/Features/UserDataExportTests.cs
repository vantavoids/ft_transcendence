using System.Net;
using System.Text.Json;
using Chat.Application.Abstractions.Persistence;
using Chat.FunctionalTests.Infrastructure;
using Xunit;

namespace Chat.FunctionalTests.Features;

public sealed class UserDataExportTests(ChatApiFactory factory) : IClassFixture<ChatApiFactory>
{
	[Fact]
	public async Task Internal_Anonymous_NotUnderV1_RejectsNonPositive()
	{
		var anon = factory.CreateClient();
		// internal group: reachable without a token, but not under /v1 (gateway-facing)
		Assert.Equal(HttpStatusCode.OK, (await anon.GetAsync("/internal/users/1/data-export")).StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, (await anon.GetAsync("/v1/users/1/data-export")).StatusCode);
		Assert.Equal(HttpStatusCode.BadRequest, (await anon.GetAsync("/internal/users/0/data-export")).StatusCode);
	}

	[Fact]
	public async Task Exports_Seeded_Channel_And_Direct_Messages()
	{
		factory.DataExportRepository.ChannelMessages.Add(
			new ExportedChannelMessage(10, 100, "hello channel", DateTimeOffset.UtcNow, null));
		factory.DataExportRepository.DirectMessages.Add(
			new ExportedDirectMessage(20, 30, 200, "hi dm", DateTimeOffset.UtcNow, null));

		var resp = await factory.CreateClient().GetAsync("/internal/users/42/data-export");
		Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

		var root = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
		Assert.Equal("42", root.GetProperty("user_id").GetString());

		var ch = Assert.Single(root.GetProperty("channel_messages").EnumerateArray());
		Assert.Equal("10", ch.GetProperty("channel_id").GetString());
		Assert.Equal("100", ch.GetProperty("message_id").GetString());
		Assert.Equal("hello channel", ch.GetProperty("content").GetString());

		var dm = Assert.Single(root.GetProperty("direct_messages").EnumerateArray());
		Assert.Equal("30", dm.GetProperty("partner_id").GetString());
		Assert.Equal("200", dm.GetProperty("message_id").GetString());
		Assert.Equal("hi dm", dm.GetProperty("content").GetString());
	}
}
