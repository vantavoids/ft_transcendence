using Chat.Application.Abstractions;

namespace Chat.UnitTests.Fakes;

public sealed class FakeUserBroadcaster : IUserBroadcaster
{
	private readonly List<(long UserId, long GuildId, string GuildName)> _joinCalls = [];
	private readonly List<(long UserId, long GuildId)> _leftCalls = [];
	private readonly List<(long UserId, long GuildId)> _evictGuildCalls = [];
	private readonly List<(long UserId, long ChannelId)> _evictChannelCalls = [];
	private readonly List<long> _disconnectCalls = [];

	public IReadOnlyList<(long UserId, long GuildId, string GuildName)> JoinCalls => _joinCalls;
	public IReadOnlyList<(long UserId, long GuildId)> LeftCalls => _leftCalls;
	public IReadOnlyList<(long UserId, long GuildId)> EvictGuildCalls => _evictGuildCalls;
	public IReadOnlyList<(long UserId, long ChannelId)> EvictChannelCalls => _evictChannelCalls;
	public IReadOnlyList<long> DisconnectCalls => _disconnectCalls;

	public Task BroadcastGuildJoinedAsync(long userId, long guildId, string guildName, CancellationToken ct)
	{
		_joinCalls.Add((userId, guildId, guildName));
		return Task.CompletedTask;
	}

	public Task BroadcastGuildLeftAsync(long userId, long guildId, CancellationToken ct)
	{
		_leftCalls.Add((userId, guildId));
		return Task.CompletedTask;
	}

	// EvictedCount is returned from both evict methods so consumer tests can
	// exercise whatever the caller does with the purged-subscription count.
	public int EvictedCount { get; set; }

	public Task<int> EvictFromGuildChannelsAsync(long userId, long guildId, CancellationToken ct)
	{
		_evictGuildCalls.Add((userId, guildId));
		return Task.FromResult(EvictedCount);
	}

	public Task<int> EvictFromChannelAsync(long userId, long channelId, CancellationToken ct)
	{
		_evictChannelCalls.Add((userId, channelId));
		return Task.FromResult(EvictedCount);
	}

	public Task<int> DisconnectUserAsync(long userId, CancellationToken ct)
	{
		_disconnectCalls.Add(userId);
		return Task.FromResult(EvictedCount);
	}
}
