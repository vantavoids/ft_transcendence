using Chat.Application.Abstractions;
using Chat.Application.Features.Channels.Common;
using Chat.Application.Features.DirectMessages.Common;

namespace Chat.UnitTests.Fakes;

public sealed class FakeUserBroadcaster : IUserBroadcaster
{
	private readonly List<(long UserId, long GuildId, string GuildName)> _joinCalls = [];
	private readonly List<(long UserId, long GuildId)> _leftCalls = [];
	private readonly List<(long UserId, long GuildId)> _addGuildGroupCalls = [];
	private readonly List<(long UserId, long GuildId)> _removeGuildGroupCalls = [];
	private readonly List<(long UserId, long GuildId)> _evictGuildCalls = [];
	private readonly List<(long UserId, long ChannelId)> _evictChannelCalls = [];
	private readonly List<long> _disconnectCalls = [];
	private readonly List<(long UserId, ChannelReadStateResponse Response)> _readStateCalls = [];
	private readonly List<(long UserId, DmReadStateResponse Response)> _dmReadStateCalls = [];

	public IReadOnlyList<(long UserId, long GuildId, string GuildName)> JoinCalls => _joinCalls;
	public IReadOnlyList<(long UserId, long GuildId)> LeftCalls => _leftCalls;
	public IReadOnlyList<(long UserId, long GuildId)> AddGuildGroupCalls => _addGuildGroupCalls;
	public IReadOnlyList<(long UserId, long GuildId)> RemoveGuildGroupCalls => _removeGuildGroupCalls;
	public IReadOnlyList<(long UserId, long GuildId)> EvictGuildCalls => _evictGuildCalls;
	public IReadOnlyList<(long UserId, long ChannelId)> EvictChannelCalls => _evictChannelCalls;
	public IReadOnlyList<long> DisconnectCalls => _disconnectCalls;
	public IReadOnlyList<(long UserId, ChannelReadStateResponse Response)> ReadStateCalls => _readStateCalls;
	public IReadOnlyList<(long UserId, DmReadStateResponse Response)> DmReadStateCalls => _dmReadStateCalls;

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

	public Task AddUserToGuildGroupAsync(long userId, long guildId, CancellationToken ct)
	{
		_addGuildGroupCalls.Add((userId, guildId));
		return Task.CompletedTask;
	}

	public Task RemoveUserFromGuildGroupAsync(long userId, long guildId, CancellationToken ct)
	{
		_removeGuildGroupCalls.Add((userId, guildId));
		return Task.CompletedTask;
	}

	public Task BroadcastChannelReadStateUpdatedAsync(long userId, ChannelReadStateResponse response, CancellationToken ct)
	{
		_readStateCalls.Add((userId, response));
		return Task.CompletedTask;
	}

	public Task BroadcastDmReadStateUpdatedAsync(long userId, DmReadStateResponse response, CancellationToken ct)
	{
		_dmReadStateCalls.Add((userId, response));
		return Task.CompletedTask;
	}

	private readonly List<(IReadOnlyList<long> UserIds, GuildChannelDto Channel)> _channelCreatedCalls = [];
	private readonly List<(IReadOnlyList<long> UserIds, GuildChannelDto Channel)> _channelUpdatedCalls = [];
	private readonly List<(IReadOnlyList<long> UserIds, long GuildId, long ChannelId)> _channelDeletedCalls = [];
	private readonly List<(long GuildId, long UserId)> _memberJoinedCalls = [];
	private readonly List<(long GuildId, long UserId)> _memberLeftCalls = [];

	public IReadOnlyList<(IReadOnlyList<long> UserIds, GuildChannelDto Channel)> ChannelCreatedCalls => _channelCreatedCalls;
	public IReadOnlyList<(IReadOnlyList<long> UserIds, GuildChannelDto Channel)> ChannelUpdatedCalls => _channelUpdatedCalls;
	public IReadOnlyList<(IReadOnlyList<long> UserIds, long GuildId, long ChannelId)> ChannelDeletedCalls => _channelDeletedCalls;
	public IReadOnlyList<(long GuildId, long UserId)> MemberJoinedCalls => _memberJoinedCalls;
	public IReadOnlyList<(long GuildId, long UserId)> MemberLeftCalls => _memberLeftCalls;

	public Task BroadcastChannelCreatedAsync(IReadOnlyList<long> userIds, GuildChannelDto channel, CancellationToken ct)
	{
		_channelCreatedCalls.Add((userIds, channel));
		return Task.CompletedTask;
	}

	public Task BroadcastChannelUpdatedAsync(IReadOnlyList<long> userIds, GuildChannelDto channel, CancellationToken ct)
	{
		_channelUpdatedCalls.Add((userIds, channel));
		return Task.CompletedTask;
	}

	public Task BroadcastChannelDeletedAsync(IReadOnlyList<long> userIds, long guildId, long channelId, CancellationToken ct)
	{
		_channelDeletedCalls.Add((userIds, guildId, channelId));
		return Task.CompletedTask;
	}

	public Task BroadcastMemberJoinedAsync(long guildId, long userId, CancellationToken ct)
	{
		_memberJoinedCalls.Add((guildId, userId));
		return Task.CompletedTask;
	}

	public Task BroadcastMemberLeftAsync(long guildId, long userId, CancellationToken ct)
	{
		_memberLeftCalls.Add((guildId, userId));
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
