using System.Collections.Concurrent;

namespace Chat.Presentation.Hubs;

public sealed class UserConnectionTracker
{
	private readonly ConcurrentDictionary<long, int> _counts = new();

	// Returns true if this is the user's first connection.
	public bool TrackConnected(long userId)
		=> _counts.AddOrUpdate(userId, 1, (_, c) => c + 1) == 1;

	// Returns true if this was the user's last connection.
	public bool TrackDisconnected(long userId)
	{
		while (true)
		{
			if (!_counts.TryGetValue(userId, out var count))
				return true;
			if (count <= 1)
			{
				if (_counts.TryRemove(new KeyValuePair<long, int>(userId, count)))
					return true;
			}
			else
			{
				if (_counts.TryUpdate(userId, count - 1, count))
					return false;
			}
		}
	}
}
