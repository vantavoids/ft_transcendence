using Cassandra;

namespace Chat.Persistence.Repositories;

/// <summary>
/// Prepared statements for channel and DM read cursors, plus the DM unread
/// COUNTER table. See ReadStateRepository for the monotonic-upsert logic
/// these statements are combined into.
/// </summary>
internal sealed class ReadStateStatements(ISession session)
{
	/* ### Channel read states ### */

	public Lazy<Task<PreparedStatement>> SelectChannelReadState { get; } = new(() => session.PrepareAsync(
		"SELECT last_read_message_id, last_read_at FROM channel_read_states " +
		"WHERE user_id = ? AND channel_id = ?"));

	public Lazy<Task<PreparedStatement>> SelectChannelReadStatesForUser { get; } = new(() => session.PrepareAsync(
		"SELECT channel_id, last_read_message_id, last_read_at FROM channel_read_states WHERE user_id = ?"));

	public Lazy<Task<PreparedStatement>> DeleteChannelReadStatesForUser { get; } = new(() => session.PrepareAsync(
		"DELETE FROM channel_read_states WHERE user_id = ?"));

	public Lazy<Task<PreparedStatement>> UpsertChannelReadState { get; } = new(() => session.PrepareAsync(
		"INSERT INTO channel_read_states (user_id, channel_id, last_read_message_id, last_read_at) " +
		"VALUES (?, ?, ?, ?)"));

	public Lazy<Task<PreparedStatement>> CountChannelMessagesAfter { get; } = new(() => session.PrepareAsync(
		"SELECT COUNT(*) FROM messages WHERE channel_id = ? AND created_at > ?"));

	/* ### DM read states ### */

	public Lazy<Task<PreparedStatement>> SelectDmReadState { get; } = new(() => session.PrepareAsync(
		"SELECT last_read_message_id, last_read_at FROM dm_read_states " +
		"WHERE user_id = ? AND partner_id = ?"));

	public Lazy<Task<PreparedStatement>> UpsertDmReadState { get; } = new(() => session.PrepareAsync(
		"INSERT INTO dm_read_states (user_id, partner_id, last_read_message_id, last_read_at) " +
		"VALUES (?, ?, ?, ?)"));

	public Lazy<Task<PreparedStatement>> DeleteDmReadStatesForUser { get; } = new(() => session.PrepareAsync(
		"DELETE FROM dm_read_states WHERE user_id = ?"));

	/* ### DM unread counter ### */

	public Lazy<Task<PreparedStatement>> IncrementDmUnreadCount { get; } = new(() => session.PrepareAsync(
		"UPDATE dm_unread_counts SET count = count + 1 WHERE user_id = ? AND partner_id = ?"));

	// counters can only be reset by deleting the row - a missing row reads back as 0
	public Lazy<Task<PreparedStatement>> ResetDmUnreadCount { get; } = new(() => session.PrepareAsync(
		"DELETE FROM dm_unread_counts WHERE user_id = ? AND partner_id = ?"));

	public Lazy<Task<PreparedStatement>> DeleteDmUnreadCountsForUser { get; } = new(() => session.PrepareAsync(
		"DELETE FROM dm_unread_counts WHERE user_id = ?"));
}
