using Cassandra;

namespace Chat.Persistence.Repositories;

/// <summary>
/// Prepared statements for both channel messages and DMs. The two message
/// types still live in separate physical tables (messages/direct_messages,
/// message_attachments/dm_attachments, message_nonce_dedup/dm_nonce_dedup) —
/// merging those is a separate, riskier migration (message_attachments is
/// partitioned by channel_id specifically so batch attachment reads stay
/// single-partition) — but the C# surface is unified here.
/// </summary>
internal sealed class MessageStatements(ISession session)
{
	/* ### Lookup ### */

	public Lazy<Task<PreparedStatement>> InsertLookup { get; } = new(() => session.PrepareAsync(
		"INSERT INTO message_lookup " +
		"(message_id, is_dm, channel_id, conversation_id, created_at, author_id) " +
		"VALUES (?, ?, ?, ?, ?, ?)"));

	public Lazy<Task<PreparedStatement>> SelectLookup { get; } = new(() => session.PrepareAsync(
		"SELECT is_dm, channel_id, conversation_id, created_at FROM message_lookup WHERE message_id = ?"));


	/* ### Channel messages ### */

	public Lazy<Task<PreparedStatement>> InsertChannelMessage { get; } = new(() => session.PrepareAsync(
		"INSERT INTO messages " +
		"(channel_id, created_at, id, author_id, content, edited_at, is_deleted, reply_to_id) " +
		"VALUES (?, ?, ?, ?, ?, ?, ?, ?)"));

	public Lazy<Task<PreparedStatement>> InsertChannelNonce { get; } = new(() => session.PrepareAsync(
		"INSERT INTO message_nonce_dedup (author_id, channel_id, nonce, message_id) VALUES (?, ?, ?, ?)"));

	public Lazy<Task<PreparedStatement>> FindChannelNonce { get; } = new(() => session.PrepareAsync(
		"SELECT message_id FROM message_nonce_dedup WHERE author_id = ? AND channel_id = ? AND nonce = ?"));

	public Lazy<Task<PreparedStatement>> UpdateChannelMessageContent { get; } = new(() => session.PrepareAsync(
		"UPDATE messages SET content = ?, edited_at = ? " +
		"WHERE channel_id = ? AND created_at = ? AND id = ?"));

	public Lazy<Task<PreparedStatement>> SoftDeleteChannelMessage { get; } = new(() => session.PrepareAsync(
		"UPDATE messages SET is_deleted = true " +
		"WHERE channel_id = ? AND created_at = ? AND id = ?"));

	// partition delete: messages are partitioned by channel_id, so removing a
	// whole channel's history is a single-partition delete. used when a guild
	// (and thus its channels) is deleted.
	public Lazy<Task<PreparedStatement>> DeleteChannelMessages { get; } = new(() => session.PrepareAsync(
		"DELETE FROM messages WHERE channel_id = ?"));

	public Lazy<Task<PreparedStatement>> SelectChannelMessage { get; } = new(() => session.PrepareAsync(
		"SELECT channel_id, created_at, id, author_id, content, edited_at, is_deleted, reply_to_id " +
		"FROM messages WHERE channel_id = ? AND created_at = ? AND id = ?"));

	public Lazy<Task<PreparedStatement>> SelectChannelMessagesRange { get; } = new(() => session.PrepareAsync(
		"SELECT channel_id, created_at, id, author_id, content, edited_at, is_deleted, reply_to_id " +
		"FROM messages WHERE channel_id = ? AND created_at < ? LIMIT ?"));

	/* ### Direct messages ### */

	public Lazy<Task<PreparedStatement>> InsertDirectMessage { get; } = new(() => session.PrepareAsync(
		"INSERT INTO direct_messages " +
		"(conversation_id, created_at, id, sender_id, recipient_id, content, edited_at, is_deleted, reply_to_id) " +
		"VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)"));

	public Lazy<Task<PreparedStatement>> InsertDmNonce { get; } = new(() => session.PrepareAsync(
		"INSERT INTO dm_nonce_dedup (sender_id, recipient_id, nonce, message_id) VALUES (?, ?, ?, ?)"));

	public Lazy<Task<PreparedStatement>> FindDmNonce { get; } = new(() => session.PrepareAsync(
		"SELECT message_id FROM dm_nonce_dedup WHERE sender_id = ? AND recipient_id = ? AND nonce = ?"));

	public Lazy<Task<PreparedStatement>> UpdateDirectMessageContent { get; } = new(() => session.PrepareAsync(
		"UPDATE direct_messages SET content = ?, edited_at = ? " +
		"WHERE conversation_id = ? AND created_at = ? AND id = ?"));

	public Lazy<Task<PreparedStatement>> SoftDeleteDirectMessage { get; } = new(() => session.PrepareAsync(
		"UPDATE direct_messages SET is_deleted = true " +
		"WHERE conversation_id = ? AND created_at = ? AND id = ?"));

	public Lazy<Task<PreparedStatement>> SelectDirectMessage { get; } = new(() => session.PrepareAsync(
		"SELECT conversation_id, created_at, id, sender_id, recipient_id, content, edited_at, is_deleted, reply_to_id " +
		"FROM direct_messages WHERE conversation_id = ? AND created_at = ? AND id = ?"));

	public Lazy<Task<PreparedStatement>> SelectDirectMessagesByConversation { get; } = new(() => session.PrepareAsync(
		"SELECT conversation_id, created_at, id, sender_id, recipient_id, content, edited_at, is_deleted, reply_to_id " +
		"FROM direct_messages WHERE conversation_id = ? AND created_at < ? LIMIT ?"));


	public Lazy<Task<PreparedStatement>> FindConversation { get; } = new(() => session.PrepareAsync(
		"SELECT conversation_id FROM user_conversations WHERE user_id = ? AND partner_id = ?"));

	public Lazy<Task<PreparedStatement>> UpsertConversation { get; } = new(() => session.PrepareAsync(
		"INSERT INTO user_conversations " +
		"(user_id, partner_id, conversation_id, last_message_at, last_preview, is_archived) " +
		"VALUES (?, ?, ?, ?, ?, false)"));

	public Lazy<Task<PreparedStatement>> DeleteConversation { get; } = new(() => session.PrepareAsync("DELETE FROM user_conversations WHERE user_id = ?"));

	// Conditional insert used by GetOrCreateConversationAsync to atomically decide
	// the conversation_id on the very first message between two users.
	// On a failed condition, Cassandra returns the current row's values (including
	// conversation_id) in the same RowSet, so no follow-up read is needed.
	public Lazy<Task<PreparedStatement>> InsertConversationIfNotExists { get; } = new(() => session.PrepareAsync(
		"INSERT INTO user_conversations " +
		"(user_id, partner_id, conversation_id, last_message_at, last_preview, is_archived) " +
		"VALUES (?, ?, ?, null, null, false) IF NOT EXISTS"));

	public Lazy<Task<PreparedStatement>> SelectConversationsByUser { get; } = new(() => session.PrepareAsync(
		"SELECT partner_id, last_message_at, last_preview, is_archived FROM user_conversations WHERE user_id = ?"));

	public Lazy<Task<PreparedStatement>> ArchiveConversation { get; } = new(() => session.PrepareAsync(
		"UPDATE user_conversations SET is_archived = true WHERE user_id = ? AND partner_id = ?"));


	/* ### Attachements ### */

	public Lazy<Task<PreparedStatement>> InsertMessageAttachment { get; } = new(() => session.PrepareAsync(
		"INSERT INTO message_attachments " +
		"(channel_id, message_id, id, url, filename, size_bytes, mime_type) " +
		"VALUES (?, ?, ?, ?, ?, ?, ?)"));

	public Lazy<Task<PreparedStatement>> InsertDmAttachment { get; } = new(() => session.PrepareAsync(
		"INSERT INTO dm_attachments " +
		"(conversation_id, message_id, id, url, filename, size_bytes, mime_type) " +
		"VALUES (?, ?, ?, ?, ?, ?, ?)"));

	public Lazy<Task<PreparedStatement>> InsertAttachmentLookup { get; } = new(() => session.PrepareAsync(
		"INSERT INTO attachment_lookup " +
		"(attachment_id, is_dm, channel_id, conversation_id, message_id) " +
		"VALUES (?, ?, ?, ?, ?)"));

	public Lazy<Task<PreparedStatement>> DeleteDraftAttachment { get; } = new(() => session.PrepareAsync(
		"DELETE FROM draft_attachments WHERE id = ?"));
}
