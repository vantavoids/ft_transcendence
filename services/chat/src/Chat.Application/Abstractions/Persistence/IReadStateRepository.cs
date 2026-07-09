using Chat.Domain.ReadStates;

namespace Chat.Application.Abstractions.Persistence;

/// <summary>
/// Persistence for per-user read cursors, backing <c>PUT /channels/{id}/read</c>,
/// <c>PUT /dms/{user_id}/read</c> and <c>GET /channels/read-states</c>. Channel
/// and DM cursors live in separate tables (<c>channel_read_states</c> /
/// <c>dm_read_states</c>) but share one C# surface via <see cref="ReadState"/>'s
/// <c>ContainerId</c> + <c>IsDm</c> discriminator, mirroring how <c>Message</c>
/// unifies channel messages and DMs.
/// </summary>
public interface IReadStateRepository
{
	/// <summary>
	/// Advances the caller's read cursor for <paramref name="containerId"/> (a channel id,
	/// or the partner's user id when <paramref name="isDm"/>) to <paramref name="messageId"/>
	/// if it is newer than the current one (or none exists yet); otherwise a no-op.
	/// Always returns the resulting (possibly unchanged) cursor.
	/// </summary>
	Task<ReadState> UpsertIfNewerAsync(
		long userId, long containerId, bool isDm, long messageId, DateTimeOffset readAt, CancellationToken ct);

	/// <summary>Every channel_read_states row for this user; backs the bulk sidebar fetch.</summary>
	Task<IReadOnlyList<ReadState>> GetChannelReadStatesForUserAsync(long userId, CancellationToken ct);

	/// <summary>
	/// Count of messages in <paramref name="channelId"/> with <c>created_at &gt; after</c>.
	/// Server-side COUNT(*) over the channel's clustering range - no ALLOW FILTERING
	/// needed since created_at is a clustering column. Soft-deleted messages are not
	/// excluded (would require ALLOW FILTERING on a non-key column over an unbounded
	/// partition); a deleted message inflates the count by at most itself until read
	/// past, an accepted skew given this is a badge count, not a ledger.
	/// </summary>
	Task<int> CountChannelMessagesAfterAsync(long channelId, DateTimeOffset after, CancellationToken ct);

	/// <summary>
	/// Resets the caller's dm_unread_counts row for this partner to 0. Cassandra
	/// counters can only be reset by deleting the row - a missing row reads back as 0.
	/// No channel equivalent: channel unread is computed on read via
	/// <see cref="CountChannelMessagesAfterAsync"/> instead of a maintained counter.
	/// </summary>
	Task ResetDmUnreadCountAsync(long userId, long partnerId, CancellationToken ct);

	/// <summary>Increments the recipient's dm_unread_counts row on a new DM send.</summary>
	Task IncrementDmUnreadCountAsync(long userId, long partnerId, CancellationToken ct);

	/// <summary>Every dm_unread_counts row for this user, keyed by partner id; backs the sidebar's per-conversation badge.</summary>
	Task<IReadOnlyDictionary<long, int>> GetDmUnreadCountsForUserAsync(long userId, CancellationToken ct);

	/// <summary>
	/// Purges every read-cursor row for this user across channel_read_states,
	/// dm_read_states and dm_unread_counts. Called on account deletion.
	/// </summary>
	Task DeleteAllForUserAsync(long userId, CancellationToken ct);
}
