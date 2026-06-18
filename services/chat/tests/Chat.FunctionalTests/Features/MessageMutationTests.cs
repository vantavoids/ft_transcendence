using Chat.Application.Features.Messages.Common;
using Chat.FunctionalTests.Infrastructure;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace Chat.FunctionalTests.Features;

// ─────────────────────────────────────────────────────────────────────────────
// Hub broadcast reception — MessageEdited
//
// FakeChannelBroadcaster replaces SignalRChannelBroadcaster in the factory, so
// PATCH via REST never reaches the real SignalR group. These tests use
// SimulateMessageEditedAsync to push the event directly into the hub group and
// verify that connected subscribers receive the right invocation.
// ─────────────────────────────────────────────────────────────────────────────

public sealed class EditMessageBroadcastTests(ChatApiFactory factory)
	: IClassFixture<ChatApiFactory>, IAsyncLifetime
{
	private const long ReadMessages = 1L << 1;
	private const long GuildId      = 999;
	private const long ChannelId    = 100;

	private const long UserSubscriber    = 5_001;
	private const long UserNonSubscriber = 5_002;
	private const long UserSender        = 5_003;

	public Task InitializeAsync()
	{
		factory.GuildClient.Result = null;
		factory.MessageRepository.Reset();
		factory.Broadcaster.Reset();
		return Task.CompletedTask;
	}

	public Task DisposeAsync() => Task.CompletedTask;

	// T2.11 & T2.12 — subscriber get MessageEdited with right payload
	[Fact]
	public async Task MessageEdited_SubscriberInGroup_ReceivesEventWithCorrectPayload()
	{
		factory.WithMembershipFor(ChannelId, UserSubscriber, GuildId, ReadMessages);
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserSubscriber);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var tcs = new TaskCompletionSource<MessageEditedEvent>();
		conn.On<MessageEditedEvent>("MessageEdited", evt => tcs.TrySetResult(evt));

		await conn.StartAsync();
		await conn.InvokeAsync("JoinChannel", ChannelId);

		var sentEvt = new MessageEditedEvent(
			Id: "42",
			ChannelId: ChannelId.ToString(),
			Content: "edited content",
			EditedAt: DateTimeOffset.UtcNow);

		await factory.SimulateMessageEditedAsync(ChannelId, sentEvt);

		var received = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.Equal("42", received.Id);
		Assert.Equal(ChannelId.ToString(), received.ChannelId);
		Assert.Equal("edited content", received.Content);
		Assert.NotEqual(default, received.EditedAt);
	}

	// T2.13 — broadcast Group SignalR : everyone receive (sender too)
	[Fact]
	public async Task MessageEdited_BroadcastIsToFullGroup_SenderAlsoReceives()
	{
		factory.WithMembershipFor(ChannelId, UserSender, GuildId, ReadMessages);
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserSender);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var received = false;
		conn.On<MessageEditedEvent>("MessageEdited", _ => received = true);

		await conn.StartAsync();
		await conn.InvokeAsync("JoinChannel", ChannelId);

		await factory.SimulateMessageEditedAsync(ChannelId,
			new MessageEditedEvent("1", ChannelId.ToString(), "x", DateTimeOffset.UtcNow));

		await Task.Delay(300);
		Assert.True(received);
	}

	// T2.14 — user not in Group → doesn't get MessageEdited
	[Fact]
	public async Task MessageEdited_NonSubscriber_DoesNotReceive()
	{
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserNonSubscriber);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var received = false;
		conn.On<MessageEditedEvent>("MessageEdited", _ => received = true);

		await conn.StartAsync();
		// intentionally no JoinChannel

		await factory.SimulateMessageEditedAsync(ChannelId,
			new MessageEditedEvent("1", ChannelId.ToString(), "x", DateTimeOffset.UtcNow));

		await Task.Delay(300);
		Assert.False(received);
	}
}

// ─────────────────────────────────────────────────────────────────────────────
// Hub broadcast reception — MessageDeleted
// ─────────────────────────────────────────────────────────────────────────────

public sealed class DeleteMessageBroadcastTests(ChatApiFactory factory)
	: IClassFixture<ChatApiFactory>, IAsyncLifetime
{
	private const long ReadMessages = 1L << 1;
	private const long GuildId      = 999;
	private const long ChannelId    = 200;

	private const long UserSubscriber    = 6_001;
	private const long UserNonSubscriber = 6_002;

	public Task InitializeAsync()
	{
		factory.GuildClient.Result = null;
		factory.MessageRepository.Reset();
		factory.Broadcaster.Reset();
		return Task.CompletedTask;
	}

	public Task DisposeAsync() => Task.CompletedTask;

	// T2.22 & T2.23 — subscriber get MessageDeleted with right payload
	[Fact]
	public async Task MessageDeleted_SubscriberInGroup_ReceivesEventWithCorrectPayload()
	{
		factory.WithMembershipFor(ChannelId, UserSubscriber, GuildId, ReadMessages);
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserSubscriber);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var tcs = new TaskCompletionSource<MessageDeletedEvent>();
		conn.On<MessageDeletedEvent>("MessageDeleted", evt => tcs.TrySetResult(evt));

		await conn.StartAsync();
		await conn.InvokeAsync("JoinChannel", ChannelId);

		await factory.SimulateMessageDeletedAsync(ChannelId, messageId: 99);

		var received = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.Equal("99", received.MessageId);
		Assert.Equal(ChannelId.ToString(), received.ChannelId);
	}

	// T2.23b — user not in Group → doesn't get MessageDeleted
	[Fact]
	public async Task MessageDeleted_NonSubscriber_DoesNotReceive()
	{
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserNonSubscriber);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var received = false;
		conn.On<MessageDeletedEvent>("MessageDeleted", _ => received = true);

		await conn.StartAsync();
		// intentionally no JoinChannel

		await factory.SimulateMessageDeletedAsync(ChannelId, messageId: 99);

		await Task.Delay(300);
		Assert.False(received);
	}
}
