using Chat.Presentation.Hubs;
using Microsoft.AspNetCore.SignalR;
using Xunit;

namespace Chat.UnitTests.Presentation;

public sealed class SignalRUserBroadcasterTests
{
	private const long User = 42;
	private const long Guild = 7;
	private const long ChannelA = 100;
	private const long ChannelB = 200;

	[Fact]
	public async Task EvictFromGuildChannels_RemovesEveryConnectionAndForgetsThem()
	{
		var tracker = new UserConnectionTracker();
		tracker.TrackConnected(User, "c1");
		tracker.TrackChannelJoined(User, "c1", ChannelA, Guild);
		tracker.TrackChannelJoined(User, "c1", ChannelB, Guild);

		var groups = new RecordingGroupManager();
		var broadcaster = new SignalRUserBroadcaster(new FakeHubContext(groups), tracker);

		var evicted = await broadcaster.EvictFromGuildChannelsAsync(User, Guild, CancellationToken.None);

		Assert.Equal(2, evicted);
		Assert.Contains(("c1", $"channel:{ChannelA}"), groups.Removed);
		Assert.Contains(("c1", $"channel:{ChannelB}"), groups.Removed);
		Assert.Empty(tracker.ConnectionsInGuild(User, Guild));
	}

	// guards the retry-safety contract: GuildMemberLeftConsumer is a MassTransit
	// consumer, so a throw mid-eviction redelivers the message. a connection whose
	// group removal failed must NOT be forgotten, or the retry can never evict it.
	[Fact]
	public async Task EvictFromGuildChannels_WhenAGroupRemovalFails_LeavesItRetryable()
	{
		var tracker = new UserConnectionTracker();
		tracker.TrackConnected(User, "c1");
		tracker.TrackChannelJoined(User, "c1", ChannelA, Guild);
		tracker.TrackChannelJoined(User, "c1", ChannelB, Guild);

		var groups = new RecordingGroupManager { FailOnGroup = $"channel:{ChannelB}" };
		var broadcaster = new SignalRUserBroadcaster(new FakeHubContext(groups), tracker);

		await Assert.ThrowsAsync<InvalidOperationException>(
			() => broadcaster.EvictFromGuildChannelsAsync(User, Guild, CancellationToken.None));

		// the connection that failed to leave is still tracked, so a retry can find it
		Assert.Equal("c1", Assert.Single(tracker.ConnectionsInChannel(User, ChannelB)));

		groups.FailOnGroup = null;
		await broadcaster.EvictFromGuildChannelsAsync(User, Guild, CancellationToken.None);

		Assert.Empty(tracker.ConnectionsInGuild(User, Guild));
		Assert.Contains(("c1", $"channel:{ChannelB}"), groups.Removed);
	}

	private sealed class RecordingGroupManager : IGroupManager
	{
		public List<(string ConnectionId, string Group)> Removed { get; } = [];
		public string? FailOnGroup { get; set; }

		public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
		{
			if (groupName == FailOnGroup)
				throw new InvalidOperationException($"group removal failed for {groupName}");
			Removed.Add((connectionId, groupName));
			return Task.CompletedTask;
		}

		public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
			=> Task.CompletedTask;
	}

	private sealed class FakeHubContext(IGroupManager groups) : IHubContext<ChatHub, IChatClient>
	{
		public IGroupManager Groups => groups;

		// eviction only touches Groups; Clients is never reached.
		public IHubClients<IChatClient> Clients => throw new NotSupportedException();
	}
}
