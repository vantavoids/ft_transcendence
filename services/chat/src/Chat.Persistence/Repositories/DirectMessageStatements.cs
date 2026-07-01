using Cassandra;

namespace Chat.Persistence.Repositories;

internal sealed class DirectMessageStatements(ISession session)
{
	public Lazy<Task<PreparedStatement>> InsertDirectMessage { get; } = new(() => session.PrepareAsync(
		"INSERT INTO direct_messages " +
		"(conversation_id, created_at, id, sender_id, recipient_id, content, edited_at, is_deleted, reply_to_id) " +
		"VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)"));

	public Lazy<Task<PreparedStatement>> InsertLookup { get; } = new(() => session.PrepareAsync(
		"INSERT INTO message_lookup " +
		"(message_id, is_dm, channel_id, conversation_id, created_at, author_id) " +
		"VALUES (?, true, ?, ?, ?, ?)"));

	public Lazy<Task<PreparedStatement>> UpsertConversation { get; } = new(() => session.PrepareAsync(
		"INSERT INTO user_conversations " +
		"(user_id, partner_id, conversation_id, last_message_at, last_preview, is_archived) " +
		"VALUES (?, ?, ?, ?, ?, false)"));

	// Conditional insert used by GetOrCreateConversationAsync to atomically decide
	// the conversation_id on the very first message between two users. The caller
	// binds (min(userA, userB), max(userA, userB)) so both send directions land on
	// the same partition/clustering key and the LWT actually serializes the race.
	// On a failed condition, Cassandra returns the current row's values (including
	// conversation_id) in the same RowSet, so no follow-up read is needed.
	public Lazy<Task<PreparedStatement>> InsertConversationIfNotExists { get; } = new(() => session.PrepareAsync(
		"INSERT INTO user_conversations " +
		"(user_id, partner_id, conversation_id, last_message_at, last_preview, is_archived) " +
		"VALUES (?, ?, ?, null, null, false) IF NOT EXISTS"));

	public Lazy<Task<PreparedStatement>> InsertNonce { get; } = new(() => session.PrepareAsync(
		"INSERT INTO dm_nonce_dedup " +
		"(sender_id, recipient_id, nonce, message_id) " +
		"VALUES (?, ?, ?, ?)"));

	public Lazy<Task<PreparedStatement>> FindNonce { get; } = new(() => session.PrepareAsync(
		"SELECT message_id FROM dm_nonce_dedup " +
		"WHERE sender_id = ? AND recipient_id = ? AND nonce = ?"));

	public Lazy<Task<PreparedStatement>> FindConversation { get; } = new(() => session.PrepareAsync(
		"SELECT conversation_id FROM user_conversations " +
		"WHERE user_id = ? AND partner_id = ?"));

	public Lazy<Task<PreparedStatement>> SelectLookup { get; } = new(() => session.PrepareAsync(
		"SELECT is_dm, conversation_id, created_at FROM message_lookup " +
		"WHERE message_id = ?"));

	public Lazy<Task<PreparedStatement>> SelectDirectMessage { get; } = new(() => session.PrepareAsync(
		"SELECT conversation_id, created_at, id, sender_id, recipient_id, content, edited_at, is_deleted, reply_to_id " +
		"FROM direct_messages " +
		"WHERE conversation_id = ? AND created_at = ? AND id = ?"));

	public Lazy<Task<PreparedStatement>> SelectDirectMessagesByConversation { get; } = new(() => session.PrepareAsync(
		"SELECT conversation_id, created_at, id, sender_id, recipient_id, content, edited_at, is_deleted, reply_to_id " +
		"FROM direct_messages " +
		"WHERE conversation_id = ? " +
		"LIMIT ?"));
}
