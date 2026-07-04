using Chat.Presentation.Hubs;
using Chat.UnitTests.Fakes;
using Xunit;

namespace Chat.UnitTests.Presentation;

public sealed class UserConnectionTrackerTests
{
	private const long User = 42;
	private const long Guild = 7;
	private const long OtherGuild = 8;
	private const long Channel = 100;
	private const long OtherChannel = 200;

	[Fact]
	public void TrackConnected_FirstConnection_ReturnsTrue()
	{
		var tracker = new UserConnectionTracker();

		Assert.True(tracker.TrackConnected(User, "c1"));
	}

	[Fact]
	public void TrackConnected_SecondConnection_ReturnsFalse()
	{
		var tracker = new UserConnectionTracker();
		tracker.TrackConnected(User, "c1");

		Assert.False(tracker.TrackConnected(User, "c2"));
	}

	[Fact]
	public void TrackDisconnected_LastConnection_ReturnsTrue()
	{
		var tracker = new UserConnectionTracker();
		tracker.TrackConnected(User, "c1");
		tracker.TrackConnected(User, "c2");

		Assert.False(tracker.TrackDisconnected(User, "c1"));
		Assert.True(tracker.TrackDisconnected(User, "c2"));
	}

	[Fact]
	public void TrackDisconnected_UnknownUser_ReturnsTrue()
	{
		var tracker = new UserConnectionTracker();

		Assert.True(tracker.TrackDisconnected(User, "c1"));
	}

	[Fact]
	public void ConnectionsInGuild_ReturnsOnlyChannelsJoinedUnderThatGuild()
	{
		var tracker = new UserConnectionTracker();
		tracker.TrackConnected(User, "c1");
		tracker.TrackConnected(User, "c2");
		tracker.TrackChannelJoined(User, "c1", Channel, Guild);
		tracker.TrackChannelJoined(User, "c2", OtherChannel, OtherGuild);

		Assert.Equal(("c1", Channel), Assert.Single(tracker.ConnectionsInGuild(User, Guild)));
		Assert.Equal(("c2", OtherChannel), Assert.Single(tracker.ConnectionsInGuild(User, OtherGuild)));
	}

	[Fact]
	public void ConnectionsInGuild_IsNonDestructive()
	{
		var tracker = new UserConnectionTracker();
		tracker.TrackConnected(User, "c1");
		tracker.TrackChannelJoined(User, "c1", Channel, Guild);

		tracker.ConnectionsInGuild(User, Guild);

		Assert.Equal(("c1", Channel), Assert.Single(tracker.ConnectionsInGuild(User, Guild)));
	}

	[Fact]
	public void ConnectionsInGuild_CoversMultipleChannelsAcrossConnections()
	{
		var tracker = new UserConnectionTracker();
		tracker.TrackConnected(User, "c1");
		tracker.TrackChannelJoined(User, "c1", Channel, Guild);
		tracker.TrackChannelJoined(User, "c1", OtherChannel, Guild);

		var inGuild = tracker.ConnectionsInGuild(User, Guild);

		Assert.Equal(2, inGuild.Count);
		Assert.Contains(("c1", Channel), inGuild);
		Assert.Contains(("c1", OtherChannel), inGuild);
	}

	[Fact]
	public void ConnectionsInChannel_ReturnsEveryConnectionJoinedToThatChannel()
	{
		var tracker = new UserConnectionTracker();
		tracker.TrackConnected(User, "c1");
		tracker.TrackConnected(User, "c2");
		tracker.TrackChannelJoined(User, "c1", Channel, Guild);
		tracker.TrackChannelJoined(User, "c2", Channel, Guild);
		tracker.TrackChannelJoined(User, "c2", OtherChannel, Guild);

		var inChannel = tracker.ConnectionsInChannel(User, Channel);

		Assert.Equal(2, inChannel.Count);
		Assert.Contains("c1", inChannel);
		Assert.Contains("c2", inChannel);
		Assert.Equal("c2", Assert.Single(tracker.ConnectionsInChannel(User, OtherChannel)));
	}

	[Fact]
	public void TrackChannelLeft_RemovesEntry_SoQueriesFindNothing()
	{
		var tracker = new UserConnectionTracker();
		tracker.TrackConnected(User, "c1");
		tracker.TrackChannelJoined(User, "c1", Channel, Guild);

		tracker.TrackChannelLeft(User, "c1", Channel);

		Assert.Empty(tracker.ConnectionsInGuild(User, Guild));
		Assert.Empty(tracker.ConnectionsInChannel(User, Channel));
	}

	[Fact]
	public void TrackDisconnected_DropsJoinedChannels()
	{
		var tracker = new UserConnectionTracker();
		tracker.TrackConnected(User, "c1");
		tracker.TrackChannelJoined(User, "c1", Channel, Guild);

		tracker.TrackDisconnected(User, "c1");

		Assert.Empty(tracker.ConnectionsInGuild(User, Guild));
	}

	[Fact]
	public void ConnectionsInGuild_UnknownUser_ReturnsEmpty()
	{
		var tracker = new UserConnectionTracker();

		Assert.Empty(tracker.ConnectionsInGuild(User, Guild));
	}

	[Fact]
	public void UserContexts_ReturnsOneContextPerConnection()
	{
		var tracker = new UserConnectionTracker();
		var ctx1 = new FakeHubCallerContext();
		var ctx2 = new FakeHubCallerContext();
		tracker.TrackConnected(User, "c1", ctx1);
		tracker.TrackConnected(User, "c2", ctx2);

		var contexts = tracker.UserContexts(User);

		Assert.Equal(2, contexts.Count);
		Assert.Contains(ctx1, contexts);
		Assert.Contains(ctx2, contexts);
	}

	[Fact]
	public void UserContexts_ConnectionTrackedWithoutContext_IsSkipped()
	{
		var tracker = new UserConnectionTracker();
		tracker.TrackConnected(User, "c1");

		Assert.Empty(tracker.UserContexts(User));
	}

	[Fact]
	public void UserContexts_AfterDisconnect_NoLongerReturnsThatConnection()
	{
		var tracker = new UserConnectionTracker();
		var ctx = new FakeHubCallerContext();
		tracker.TrackConnected(User, "c1", ctx);

		tracker.TrackDisconnected(User, "c1");

		Assert.Empty(tracker.UserContexts(User));
	}

	[Fact]
	public void UserContexts_UnknownUser_ReturnsEmpty()
	{
		var tracker = new UserConnectionTracker();

		Assert.Empty(tracker.UserContexts(User));
	}
}
