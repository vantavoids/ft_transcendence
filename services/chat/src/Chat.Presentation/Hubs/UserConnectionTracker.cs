using System.Collections.Concurrent;

namespace Chat.Presentation.Hubs;

/// <summary>
/// tracks, per user, which connections are open and which <c>channel:{id}</c>
/// groups each connection has joined (along with the channel's guild). presence
/// (first/last connection) is derived from the per-user connection set, and the
/// channel bookkeeping powers server-side eviction: events only carry a userId,
/// but SignalR group removal needs a connectionId + group name, so we keep the
/// connectionId -> joined-channels mapping ourselves.
/// </summary>
public sealed class UserConnectionTracker
{
	// per-user state. Each connection maps to the set of channels it has joined
	// (channelId -> guildId). all reads/writes are guarded by locking the
	// instance, and the instance is removed from _users only while holding its
	// lock, so the connect/disconnect race is closed (see TrackConnected).
	private sealed class UserState
	{
		public readonly Dictionary<string, Dictionary<long, long>> Connections = [];
	}

	private readonly ConcurrentDictionary<long, UserState> _users = new();

	// number of users with at least one open connection (presence gauge). a user
	// is only kept in _users while it holds a connection, so this is the online
	// count. ConcurrentDictionary.Count briefly takes the map's internal locks,
	// which is fine for an infrequent scrape.
	public int OnlineUserCount => _users.Count;

	// returns true if this is the user's first connection.
	public bool TrackConnected(long userId, string connectionId)
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
				state.Connections[connectionId] = [];
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
			if (state.Connections.TryGetValue(connectionId, out var channels))
				channels[channelId] = guildId;
		}
	}

	public void TrackChannelLeft(long userId, string connectionId, long channelId)
	{
		if (!_users.TryGetValue(userId, out var state))
			return;

		lock (state)
		{
			if (state.Connections.TryGetValue(connectionId, out var channels))
				channels.Remove(channelId);
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
			foreach (var (connectionId, channels) in state.Connections)
				foreach (var (channelId, joinedGuildId) in channels)
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
			foreach (var (connectionId, channels) in state.Connections)
				if (channels.ContainsKey(channelId))
					matches.Add(connectionId);
		}
		return matches;
	}
}
