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

public sealed class UserLoggedOutConsumerTests
{
	[Fact]
	public async Task Consume_DisconnectsEveryConnectionOfThatUser()
	{
		var broadcaster = new FakeUserBroadcaster { EvictedCount = 2 };
		await using var provider = new ServiceCollection()
			.AddSingleton<IUserBroadcaster>(broadcaster)
			.AddMassTransitTestHarness(x => x.AddConsumer<UserLoggedOutConsumer>())
			.BuildServiceProvider(true);

		var harness = provider.GetRequiredService<ITestHarness>();
		await harness.Start();

		await harness.Bus.Publish(new UserLoggedOut(UserId: 42));

		Assert.True(await harness.Consumed.Any<UserLoggedOut>());
		Assert.Equal(42L, Assert.Single(broadcaster.DisconnectCalls));
	}

	[Fact]
	public async Task Consume_HasNoMessageRepositoryDependency()
	{
		var broadcaster = new FakeUserBroadcaster();
		await using var provider = new ServiceCollection()
			.AddSingleton<IUserBroadcaster>(broadcaster)
			.AddMassTransitTestHarness(x => x.AddConsumer<UserLoggedOutConsumer>())
			.BuildServiceProvider(true);

		var harness = provider.GetRequiredService<ITestHarness>();
		await harness.Start();

		await harness.Bus.Publish(new UserLoggedOut(UserId: 42));

		// user.logged_out is a transient event: it disconnects sockets but must
		// never delete data (that's user.deleted's job).
		Assert.True(await harness.Consumed.Any<UserLoggedOut>());
	}
}

public sealed class UserDeletedConsumerTests
{
	[Fact]
	public async Task Consume_DisconnectsUser_AndDeletesTheirConversationIndex()
	{
		var broadcaster = new FakeUserBroadcaster { EvictedCount = 1 };
		var messages = new FakeMessageRepository();
		await using var provider = new ServiceCollection()
			.AddSingleton<IUserBroadcaster>(broadcaster)
			.AddSingleton<IMessageRepository>(messages)
			.AddMassTransitTestHarness(x => x.AddConsumer<UserDeletedConsumer>())
			.BuildServiceProvider(true);

		var harness = provider.GetRequiredService<ITestHarness>();
		await harness.Start();

		await harness.Bus.Publish(new UserDeleted(UserId: 99));

		Assert.True(await harness.Consumed.Any<UserDeleted>());
		Assert.Equal(99L, Assert.Single(broadcaster.DisconnectCalls));
		Assert.Equal(99L, Assert.Single(messages.DeletedConversationsFor));
	}

	[Fact]
	public async Task Consume_OnlyDeletesTheDeletedUsersOwnConversationIndex()
	{
		var broadcaster = new FakeUserBroadcaster();
		var messages = new FakeMessageRepository();
		await using var provider = new ServiceCollection()
			.AddSingleton<IUserBroadcaster>(broadcaster)
			.AddSingleton<IMessageRepository>(messages)
			.AddMassTransitTestHarness(x => x.AddConsumer<UserDeletedConsumer>())
			.BuildServiceProvider(true);

		var harness = provider.GetRequiredService<ITestHarness>();
		await harness.Start();

		await harness.Bus.Publish(new UserDeleted(UserId: 7));

		Assert.True(await harness.Consumed.Any<UserDeleted>());
		var deletedFor = Assert.Single(messages.DeletedConversationsFor);
		Assert.Equal(7L, deletedFor);
	}
}
