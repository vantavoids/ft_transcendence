using Chat.Application.Abstractions.Persistence;
using Chat.Domain.ReadStates;

namespace Chat.UnitTests.Fakes;

public sealed class FakeReadStateRepository : IReadStateRepository
{
	private readonly Dictionary<(long UserId, long ContainerId, bool IsDm), ReadState> _states = [];
	private readonly Dictionary<(long UserId, long PartnerId), int> _dmUnreadCounts = [];

	/// <summary>lets a test control what CountChannelMessagesAfterAsync returns per channel</summary>
	public Dictionary<long, int> ChannelMessageCountsAfter { get; } = [];

	public List<(long UserId, long PartnerId)> IncrementedDmUnreadCounts { get; } = [];
	public List<(long UserId, long PartnerId)> ResetDmUnreadCounts { get; } = [];
	public List<long> DeletedAllForUsers { get; } = [];

	/// <summary>records every CountChannelMessagesAfterAsync call so tests can assert the exact cursor passed in</summary>
	public List<(long ChannelId, DateTimeOffset After)> CountCalls { get; } = [];

	public void SeedChannelReadState(long userId, ReadState state) =>
		_states[(userId, state.ContainerId, false)] = state;

	public void SeedDmUnreadCount(long userId, long partnerId, int count) =>
		_dmUnreadCounts[(userId, partnerId)] = count;

	public void Reset()
	{
		_states.Clear();
		ChannelMessageCountsAfter.Clear();
		IncrementedDmUnreadCounts.Clear();
		ResetDmUnreadCounts.Clear();
		CountCalls.Clear();
		DeletedAllForUsers.Clear();
		_dmUnreadCounts.Clear();
	}

	public Task<ReadState> UpsertIfNewerAsync(
		long userId, long containerId, bool isDm, long messageId, DateTimeOffset readAt, CancellationToken ct)
	{
		var key = (userId, containerId, isDm);
		if (_states.TryGetValue(key, out var current) && current.LastReadMessageId >= messageId)
			return Task.FromResult(current);

		var updated = new ReadState(containerId, isDm, messageId, readAt);
		_states[key] = updated;
		return Task.FromResult(updated);
	}

	public Task<IReadOnlyList<ReadState>> GetChannelReadStatesForUserAsync(long userId, CancellationToken ct)
	{
		IReadOnlyList<ReadState> states = _states
			.Where(kv => kv.Key.UserId == userId && !kv.Key.IsDm)
			.Select(kv => kv.Value)
			.ToList();
		return Task.FromResult(states);
	}

	public Task<int> CountChannelMessagesAfterAsync(long channelId, DateTimeOffset after, CancellationToken ct)
	{
		CountCalls.Add((channelId, after));
		return Task.FromResult(ChannelMessageCountsAfter.GetValueOrDefault(channelId, 0));
	}

	public Task ResetDmUnreadCountAsync(long userId, long partnerId, CancellationToken ct)
	{
		ResetDmUnreadCounts.Add((userId, partnerId));
		return Task.CompletedTask;
	}

	public Task IncrementDmUnreadCountAsync(long userId, long partnerId, CancellationToken ct)
	{
		IncrementedDmUnreadCounts.Add((userId, partnerId));
		return Task.CompletedTask;
	}

	public Task<IReadOnlyDictionary<long, int>> GetDmUnreadCountsForUserAsync(long userId, CancellationToken ct)
	{
		IReadOnlyDictionary<long, int> counts = _dmUnreadCounts
			.Where(kv => kv.Key.UserId == userId)
			.ToDictionary(kv => kv.Key.PartnerId, kv => kv.Value);
		return Task.FromResult(counts);
	}

	public Task DeleteAllForUserAsync(long userId, CancellationToken ct)
	{
		DeletedAllForUsers.Add(userId);
		foreach (var key in _states.Keys.Where(k => k.UserId == userId).ToList())
			_states.Remove(key);
		return Task.CompletedTask;
	}
}
