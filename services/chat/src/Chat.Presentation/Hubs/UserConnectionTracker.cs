using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace Chat.Presentation.Hubs;

/// <summary>
/// tracks, per user, which connections are open and which <c>channel:{id}</c>
/// groups each connection has joined (along with the channel's guild). presence
/// (first/last connection) is derived from the per-user connection set, and the
/// channel bookkeeping powers server-side eviction: events only carry a userId,
/// but SignalR group removal needs a connectionId + group name, so we keep the
/// connectionId -> joined-channels mapping ourselves. each connection also keeps
/// the <see cref="HubCallerContext"/> it was opened with, so a userId-only event
/// (user.logged_out/user.deleted) can force-terminate the transport via
/// <see cref="HubCallerContext.Abort"/> without relying on client cooperation.
/// </summary>
public sealed class UserConnectionTracker
{
	private sealed class ConnectionState
	{
		public HubCallerContext? Context { get; init; }
		public Dictionary<long, long> Channels { get; } = [];
	}

	// per-user state. all reads/writes are guarded by locking the instance, and
	// the instance is removed from _users only while holding its lock, so the
	// connect/disconnect race is closed (see TrackConnected).
	private sealed class UserState
	{
		public readonly Dictionary<string, ConnectionState> Connections = [];
	}

	private readonly ConcurrentDictionary<long, UserState> _users = new();

	// returns true if this is the user's first connection. context is the
	// caller's HubCallerContext, kept so the connection can later be aborted
	// from outside the Hub.
	public bool TrackConnected(long userId, string connectionId, HubCallerContext? context = null)
	{
		while (true)
		{
			var state = _users.GetOrAdd(userId, _ => new UserState());
			lock (state)
			{
				// a concurrent last-disconnect may have removed this instance
				// between GetOrAdd and the lock; retry with a fresh one.
				if (!_users.TryGetValue(userId, out var current) || !ReferenceEquals(current, state))
					continue;

				var wasEmpty = state.Connections.Count == 0;
				state.Connections[connectionId] = new ConnectionState { Context = context };
				return wasEmpty;
			}
		}
	}

	// returns true if this was the user's last connection.
	public bool TrackDisconnected(long userId, string connectionId)
	{
		if (!_users.TryGetValue(userId, out var state))
			return true;

		lock (state)
		{
			state.Connections.Remove(connectionId);
			if (state.Connections.Count == 0)
			{
				_users.TryRemove(userId, out _);
				return true;
			}
			return false;
		}
	}

	public void TrackChannelJoined(long userId, string connectionId, long channelId, long guildId)
	{
		if (!_users.TryGetValue(userId, out var state))
			return;

		lock (state)
		{
			if (state.Connections.TryGetValue(connectionId, out var conn))
				conn.Channels[channelId] = guildId;
		}
	}

	public void TrackChannelLeft(long userId, string connectionId, long channelId)
	{
		if (!_users.TryGetValue(userId, out var state))
			return;

		lock (state)
		{
			if (state.Connections.TryGetValue(connectionId, out var conn))
				conn.Channels.Remove(channelId);
		}
	}

	// snapshot of every (connection, channel) the user has joined under the given
	// guild. pure read: the caller removes these from their SignalR groups and
	// then calls TrackChannelLeft, so a mid-way failure can be safely retried.
	public IReadOnlyList<(string ConnectionId, long ChannelId)> ConnectionsInGuild(long userId, long guildId)
	{
		if (!_users.TryGetValue(userId, out var state))
			return [];

		var matches = new List<(string, long)>();
		lock (state)
		{
			foreach (var (connectionId, conn) in state.Connections)
				foreach (var (channelId, joinedGuildId) in conn.Channels)
					if (joinedGuildId == guildId)
						matches.Add((connectionId, channelId));
		}
		return matches;
	}

	public IReadOnlyList<string> ConnectionsInChannel(long userId, long channelId)
	{
		if (!_users.TryGetValue(userId, out var state))
			return [];

		var matches = new List<string>();
		lock (state)
		{
			foreach (var (connectionId, conn) in state.Connections)
				if (conn.Channels.ContainsKey(channelId))
					matches.Add(connectionId);
		}
		return matches;
	}

	// snapshot of every HubCallerContext the user currently has open, connections
	// tracked without a context (e.g. tests that call TrackConnected without one)
	// are skipped.
	public IReadOnlyList<HubCallerContext> UserContexts(long userId)
	{
		if (!_users.TryGetValue(userId, out var state))
			return [];

		lock (state)
		{
			return state.Connections.Values
				.Select(c => c.Context)
				.Where(c => c is not null)
				.Select(c => c!)
				.ToList();
		}
	}
}
