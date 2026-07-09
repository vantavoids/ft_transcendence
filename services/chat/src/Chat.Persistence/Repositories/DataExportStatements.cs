using Cassandra;

namespace Chat.Persistence.Repositories;

internal sealed class DataExportStatements(ISession session)
{
	// channels the user has a read cursor in - their per-user channel index
	public Lazy<Task<PreparedStatement>> SelectUserChannels { get; } = new(() => session.PrepareAsync(
		"SELECT channel_id FROM channel_read_states WHERE user_id = ?"));

	// the user's own messages inside one channel partition (single-partition filter)
	public Lazy<Task<PreparedStatement>> SelectAuthoredChannelMessages { get; } = new(() => session.PrepareAsync(
		"SELECT id, content, created_at, edited_at, is_deleted " +
		"FROM messages WHERE channel_id = ? AND author_id = ? ALLOW FILTERING"));

	// DM conversations the user is part of - their per-user conversation index
	public Lazy<Task<PreparedStatement>> SelectUserConversations { get; } = new(() => session.PrepareAsync(
		"SELECT partner_id, conversation_id FROM user_conversations WHERE user_id = ?"));

	// the user's own messages inside one conversation partition (single-partition filter)
	public Lazy<Task<PreparedStatement>> SelectAuthoredDmMessages { get; } = new(() => session.PrepareAsync(
		"SELECT id, content, created_at, edited_at, is_deleted " +
		"FROM direct_messages WHERE conversation_id = ? AND sender_id = ? ALLOW FILTERING"));
}
