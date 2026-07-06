using Cassandra;

namespace Chat.Persistence.Repositories;

internal sealed class ReactionStatements(ISession session)
{
	public Lazy<Task<PreparedStatement>> InsertReactionIfNotExists { get; } = new(() => session.PrepareAsync(
		"INSERT INTO message_reactions (channel_id, message_id, emoji, user_id, created_at) " +
		"VALUES (?, ?, ?, ?, ?) IF NOT EXISTS"));

	public Lazy<Task<PreparedStatement>> DeleteReactionIfExists { get; } = new(() => session.PrepareAsync(
		"DELETE FROM message_reactions WHERE channel_id = ? AND message_id = ? AND emoji = ? AND user_id = ? IF EXISTS"));

	public Lazy<Task<PreparedStatement>> IncrementCount { get; } = new(() => session.PrepareAsync(
		"UPDATE message_reaction_counts SET count = count + 1 WHERE channel_id = ? AND message_id = ? AND emoji = ?"));

	public Lazy<Task<PreparedStatement>> DecrementCount { get; } = new(() => session.PrepareAsync(
		"UPDATE message_reaction_counts SET count = count - 1 WHERE channel_id = ? AND message_id = ? AND emoji = ?"));

	public Lazy<Task<PreparedStatement>> SelectCount { get; } = new(() => session.PrepareAsync(
		"SELECT count FROM message_reaction_counts WHERE channel_id = ? AND message_id = ? AND emoji = ?"));

	public Lazy<Task<PreparedStatement>> SelectCountsForMessage { get; } = new(() => session.PrepareAsync(
		"SELECT emoji, count FROM message_reaction_counts WHERE channel_id = ? AND message_id = ?"));

	// same ALLOW FILTERING trade-off as SelectMineForMessages, scoped to a single message
	public Lazy<Task<PreparedStatement>> SelectMineForMessage { get; } = new(() => session.PrepareAsync(
		"SELECT emoji FROM message_reactions WHERE channel_id = ? AND message_id = ? AND user_id = ? ALLOW FILTERING"));

	// same IN-on-trailing-partition-key trick as message_attachments' page reads
	public Lazy<Task<PreparedStatement>> SelectCountsForMessages { get; } = new(() => session.PrepareAsync(
		"SELECT message_id, emoji, count FROM message_reaction_counts WHERE channel_id = ? AND message_id IN ?"));

	// filters on user_id while skipping the leading emoji clustering column, so it
	// needs ALLOW FILTERING - safe here because the IN-list already bounds the scan
	// to one page's worth of message partitions under a single channel_id
	public Lazy<Task<PreparedStatement>> SelectMineForMessages { get; } = new(() => session.PrepareAsync(
		"SELECT message_id, emoji FROM message_reactions " +
		"WHERE channel_id = ? AND message_id IN ? AND user_id = ? ALLOW FILTERING"));
}
