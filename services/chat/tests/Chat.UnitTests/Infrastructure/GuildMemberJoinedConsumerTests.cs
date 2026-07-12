using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Persistence;
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

		var call = Assert.Single(broadcaster.JoinCalls);
		Assert.Equal(42L, call.UserId);
		Assert.Equal(100L, call.GuildId);
		Assert.Equal("skafenings", call.GuildName);
	}
}

public sealed class GuildMemberLeftConsumerTests
{
	[Fact]
	public async Task Consume_EvictsFromGuildChannels_AndNotifiesClient()
	{
		var broadcaster = new FakeUserBroadcaster();
		await using var provider = new ServiceCollection()
			.AddSingleton<IUserBroadcaster>(broadcaster)
			.AddMassTransitTestHarness(x => x.AddConsumer<GuildMemberLeftConsumer>())
			.BuildServiceProvider(true);

		var harness = provider.GetRequiredService<ITestHarness>();
		await harness.Start();

		await harness.Bus.Publish(new GuildMemberLeft(GuildId: 100, UserId: 42));

		Assert.True(await harness.Consumed.Any<GuildMemberLeft>());

		var (UserId, GuildId) = Assert.Single(broadcaster.EvictGuildCalls);
		Assert.Equal(42L, UserId);
		Assert.Equal(100L, GuildId);

		var left = Assert.Single(broadcaster.LeftCalls);
		Assert.Equal(42L, left.UserId);
		Assert.Equal(100L, left.GuildId);
	}
}

public sealed class GuildDeletedConsumerTests
{
	[Fact]
	public async Task Consume_PurgesChannelMessages_AndBroadcastsGuildDeleted()
	{
		var broadcaster = new FakeUserBroadcaster();
		var messages = new FakeMessageRepository();
		await using var provider = new ServiceCollection()
			.AddSingleton<IUserBroadcaster>(broadcaster)
			.AddSingleton<IMessageRepository>(messages)
			.AddMassTransitTestHarness(x => x.AddConsumer<GuildDeletedConsumer>())
			.BuildServiceProvider(true);

		var harness = provider.GetRequiredService<ITestHarness>();
		await harness.Start();

		await harness.Bus.Publish(new GuildDeleted(GuildId: 100, ChannelIds: [10, 20]));

		Assert.True(await harness.Consumed.Any<GuildDeleted>());

		Assert.Equal(new long[] { 10, 20 }, messages.DeletedChannelMessagesFor);
		var guildId = Assert.Single(broadcaster.GuildDeletedCalls);
		Assert.Equal(100L, guildId);
	}
}

public sealed class ChannelAccessRevokedConsumerTests
{
	[Fact]
	public async Task Consume_EvictsUserFromSingleChannel()
	{
		var broadcaster = new FakeUserBroadcaster();
		await using var provider = new ServiceCollection()
			.AddSingleton<IUserBroadcaster>(broadcaster)
			.AddMassTransitTestHarness(x => x.AddConsumer<ChannelAccessRevokedConsumer>())
			.BuildServiceProvider(true);

		var harness = provider.GetRequiredService<ITestHarness>();
		await harness.Start();

		await harness.Bus.Publish(new ChannelAccessRevoked(ChannelId: 500, UserId: 42));

		Assert.True(await harness.Consumed.Any<ChannelAccessRevoked>());

		var (UserId, ChannelId) = Assert.Single(broadcaster.EvictChannelCalls);
		Assert.Equal(42L, UserId);
		Assert.Equal(500L, ChannelId);
	}
}

public sealed class ChannelAccessGrantedConsumerTests
{
	[Fact]
	public async Task Consume_NotifiesUserOfGrantedChannel()
	{
		var broadcaster = new FakeUserBroadcaster();
		await using var provider = new ServiceCollection()
			.AddSingleton<IUserBroadcaster>(broadcaster)
			.AddMassTransitTestHarness(x => x.AddConsumer<ChannelAccessGrantedConsumer>())
			.BuildServiceProvider(true);

		var harness = provider.GetRequiredService<ITestHarness>();
		await harness.Start();

		await harness.Bus.Publish(new ChannelAccessGranted(ChannelId: 500, GuildId: 100, UserId: 42));

		Assert.True(await harness.Consumed.Any<ChannelAccessGranted>());

		var call = Assert.Single(broadcaster.ChannelAccessGrantedCalls);
		Assert.Equal(42L, call.UserId);
		Assert.Equal(100L, call.GuildId);
		Assert.Equal(500L, call.ChannelId);
	}
}
