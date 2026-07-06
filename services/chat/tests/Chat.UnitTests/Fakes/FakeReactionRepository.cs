using Chat.Application.Abstractions.Persistence;
using Chat.Domain.Reactions;

namespace Chat.UnitTests.Fakes;

/// <summary>
/// in-memory <see cref="IReactionRepository"/>. mirrors the real repo's per-user
/// set semantics: adding/removing returns whether the set actually changed, so
/// handler idempotency (no-op repeat calls, no double broadcast) is exercisable
/// </summary>
public sealed class FakeReactionRepository : IReactionRepository
{
	private readonly Dictionary<(long ChannelId, long MessageId, string Emoji), HashSet<long>> _reactions = [];

	public void Reset() => _reactions.Clear();

	/// <summary>seed an existing reaction, e.g. to test the idempotent no-op path</summary>
	public void Seed(long channelId, long messageId, string emoji, long userId)
	{
		if (!_reactions.TryGetValue((channelId, messageId, emoji), out var set))
			_reactions[(channelId, messageId, emoji)] = set = [];
		set.Add(userId);
	}

	public Task<bool> AddAsync(long channelId, long messageId, long userId, string emoji, DateTimeOffset now, CancellationToken ct)
	{
		if (!_reactions.TryGetValue((channelId, messageId, emoji), out var set))
			_reactions[(channelId, messageId, emoji)] = set = [];
		return Task.FromResult(set.Add(userId));
	}

	public Task<bool> RemoveAsync(long channelId, long messageId, long userId, string emoji, CancellationToken ct)
	{
		var applied = _reactions.TryGetValue((channelId, messageId, emoji), out var set) && set.Remove(userId);
		return Task.FromResult(applied);
	}

	public Task<long> GetCountAsync(long channelId, long messageId, string emoji, CancellationToken ct)
	{
		var count = _reactions.TryGetValue((channelId, messageId, emoji), out var set) ? set.Count : 0;
		return Task.FromResult((long)count);
	}

	public Task<IReadOnlyList<ReactionSummary>> GetMessageReactionsAsync(
		long channelId, long messageId, long currentUserId, CancellationToken ct)
	{
		IReadOnlyList<ReactionSummary> result = _reactions
			.Where(e => e.Key.ChannelId == channelId && e.Key.MessageId == messageId)
			.Select(e => new ReactionSummary(e.Key.Emoji, e.Value.Count, e.Value.Contains(currentUserId)))
			.ToList();

		return Task.FromResult(result);
	}

	public Task<ILookup<long, ReactionSummary>> GetMessagesReactionsAsync(
		long channelId, IReadOnlyList<long> messageIds, long currentUserId, CancellationToken ct)
	{
		var wanted = messageIds.ToHashSet();

		var lookup = _reactions
			.Where(e => e.Key.ChannelId == channelId && wanted.Contains(e.Key.MessageId))
			.Select(e => (e.Key.MessageId, Summary: new ReactionSummary(e.Key.Emoji, e.Value.Count, e.Value.Contains(currentUserId))))
			.ToLookup(x => x.MessageId, x => x.Summary);

		return Task.FromResult(lookup);
	}
}
