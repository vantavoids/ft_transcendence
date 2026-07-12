using Chat.Application.Abstractions;
using Chat.Infrastructure.Messaging.Consumers;
using Chat.Infrastructure.Messaging.Contracts;
using Chat.UnitTests.Fakes;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Chat.UnitTests.Infrastructure;

public sealed class ChannelConsumersTests
{
	private static ChannelPayload SamplePayload() => new(
		Id: "500",
		GuildId: "100",
		CategoryId: null,
		Name: "general",
		Topic: null,
		Type: "text",
		Position: 0,
		IsNsfw: false,
		SlowmodeSeconds: 0);

	[Fact]
	public async Task ChannelCreated_BroadcastsChannelToEligibleUsers()
	{
		var broadcaster = new FakeUserBroadcaster();
		await using var provider = new ServiceCollection()
			.AddSingleton<IUserBroadcaster>(broadcaster)
			.AddMassTransitTestHarness(x => x.AddConsumer<GuildChannelCreatedConsumer>())
			.BuildServiceProvider(true);

		var harness = provider.GetRequiredService<ITestHarness>();
		await harness.Start();

		await harness.Bus.Publish(new GuildChannelCreated(
			GuildId: 100, Channel: SamplePayload(), EligibleUserIds: [1, 2]));

		Assert.True(await harness.Consumed.Any<GuildChannelCreated>());

		var call = Assert.Single(broadcaster.ChannelCreatedCalls);
		Assert.Equal(new long[] { 1, 2 }, call.UserIds);
		Assert.Equal("500", call.Channel.Id);
		Assert.Equal("general", call.Channel.Name);
	}

	[Fact]
	public async Task ChannelDeleted_BroadcastsIdsToEligibleUsers()
	{
		var broadcaster = new FakeUserBroadcaster();
		await using var provider = new ServiceCollection()
			.AddSingleton<IUserBroadcaster>(broadcaster)
			.AddMassTransitTestHarness(x => x.AddConsumer<GuildChannelDeletedConsumer>())
			.BuildServiceProvider(true);

		var harness = provider.GetRequiredService<ITestHarness>();
		await harness.Start();

		await harness.Bus.Publish(new GuildChannelDeleted(
			GuildId: 100, ChannelId: 500, EligibleUserIds: [7]));

		Assert.True(await harness.Consumed.Any<GuildChannelDeleted>());

		var call = Assert.Single(broadcaster.ChannelDeletedCalls);
		Assert.Equal(new long[] { 7 }, call.UserIds);
		Assert.Equal(100L, call.GuildId);
		Assert.Equal(500L, call.ChannelId);
	}
}
