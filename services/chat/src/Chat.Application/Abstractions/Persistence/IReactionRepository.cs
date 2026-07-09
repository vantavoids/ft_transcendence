using Chat.Domain.Reactions;

namespace Chat.Application.Abstractions.Persistence;

/// <summary>
/// Persistence for channel message reactions. Backed by two ScyllaDB tables:
/// <c>message_reactions</c> (per-user row, doubles as the idempotency guard)
/// and <c>message_reaction_counts</c> (COUNTER, updated only when the guard
/// row's insert/delete actually applied, so repeat calls never drift the count).
/// </summary>
public interface IReactionRepository
{
	/// <summary>true if this call added the reaction; false if the caller had already reacted (no-op)</summary>
	Task<bool> AddAsync(long channelId, long messageId, long userId, string emoji, DateTimeOffset now, CancellationToken ct);

	/// <summary>true if this call removed the reaction; false if the caller had not reacted (no-op)</summary>
	Task<bool> RemoveAsync(long channelId, long messageId, long userId, string emoji, CancellationToken ct);

	Task<long> GetCountAsync(long channelId, long messageId, string emoji, CancellationToken ct);

	Task<IReadOnlyList<ReactionSummary>> GetMessageReactionsAsync(long channelId, long messageId, long currentUserId, CancellationToken ct);

	Task<ILookup<long, ReactionSummary>> GetMessagesReactionsAsync(
		long channelId, IReadOnlyList<long> messageIds, long currentUserId, CancellationToken ct);
}
