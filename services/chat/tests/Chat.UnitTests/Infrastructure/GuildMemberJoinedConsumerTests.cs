using Chat.Application.Abstractions;
using Chat.Infrastructure.Messaging.Consumers;
using Chat.Infrastructure.Messaging.Contracts;
using Chat.UnitTests.Fakes;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Chat.UnitTests.Infrastructure;

public sealed class GuildMemberJoinedConsumerTests
{
	[Fact]
	public async Task Consume_ForwardsMessageFieldsToBroadcaster()
	{
		var broadcaster = new FakeUserBroadcaster();
		await using var provider = new ServiceCollection()
			.AddSingleton<IUserBroadcaster>(broadcaster)
			.AddMassTransitTestHarness(x => x.AddConsumer<GuildMemberJoinedConsumer>())
			.BuildServiceProvider(true);

		var harness = provider.GetRequiredService<ITestHarness>();
		await harness.Start();

		await harness.Bus.Publish(new GuildMemberJoined(
			GuildId: 100, GuildName: "skafenings", UserId: 42));

		Assert.True(await harness.Consumed.Any<GuildMemberJoined>());

		var call = Assert.Single(broadcaster.Calls);
		Assert.Equal(42L, call.UserId);
		Assert.Equal(100L, call.GuildId);
		Assert.Equal("skafenings", call.GuildName);
	}
}
