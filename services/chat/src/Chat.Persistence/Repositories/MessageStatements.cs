using Cassandra;

namespace Chat.Persistence.Repositories;

internal sealed class MessageStatements(ISession session)
{
	public Lazy<Task<PreparedStatement>> InsertMessage { get; } = new(() => session.PrepareAsync(
		"INSERT INTO messages " +
		"(channel_id, created_at, id, author_id, content, edited_at, is_deleted, reply_to_id) " +
		"VALUES (?, ?, ?, ?, ?, ?, ?, ?)"));

	public Lazy<Task<PreparedStatement>> InsertLookup { get; } = new(() => session.PrepareAsync(
		"INSERT INTO message_lookup " +
		"(message_id, is_dm, channel_id, conversation_id, created_at, author_id) " +
		"VALUES (?, false, ?, ?, ?, ?)"));

	public Lazy<Task<PreparedStatement>> InsertNonce { get; } = new(() => session.PrepareAsync(
		"INSERT INTO message_nonce_dedup (author_id, channel_id, nonce, message_id) VALUES (?, ?, ?, ?)"));

	public Lazy<Task<PreparedStatement>> FindNonce { get; } = new(() => session.PrepareAsync(
		"SELECT message_id FROM message_nonce_dedup WHERE author_id = ? AND channel_id = ? AND nonce = ?"));

	public Lazy<Task<PreparedStatement>> SelectLookup { get; } = new(() => session.PrepareAsync(
		"SELECT channel_id, created_at FROM message_lookup WHERE message_id = ?"));

	public Lazy<Task<PreparedStatement>> SelectMessage { get; } = new(() => session.PrepareAsync(
		"SELECT channel_id, created_at, id, author_id, content, edited_at, is_deleted, reply_to_id " +
		"FROM messages WHERE channel_id = ? AND created_at = ? AND id = ?"));

	public Lazy<Task<PreparedStatement>> UpdateContent { get; } = new(() => session.PrepareAsync(
		"UPDATE messages SET content = ?, edited_at = ? " +
		"WHERE channel_id = ? AND created_at = ? AND id = ?"));

	public Lazy<Task<PreparedStatement>> SoftDeleteMessage { get; } = new(() => session.PrepareAsync(
		"UPDATE messages SET is_deleted = true " +
		"WHERE channel_id = ? AND created_at = ? AND id = ?"));

	public Lazy<Task<PreparedStatement>> SelectChannelMessages { get; } = new(() => session.PrepareAsync(
		"SELECT channel_id, created_at, id, author_id, content, edited_at, is_deleted, reply_to_id " +
		"FROM messages WHERE channel_id = ? AND created_at < ? LIMIT ?"));

	public Lazy<Task<PreparedStatement>> InsertMessageAttachment { get; } = new(() => session.PrepareAsync(
		"INSERT INTO message_attachments " +
		"(channel_id, message_id, id, url, filename, size_bytes, mime_type) " +
		"VALUES (?, ?, ?, ?, ?, ?, ?)"));

	public Lazy<Task<PreparedStatement>> InsertAttachmentLookup { get; } = new(() => session.PrepareAsync(
		"INSERT INTO attachment_lookup " +
		"(attachment_id, is_dm, channel_id, conversation_id, message_id) " +
		"VALUES (?, false, ?, ?, ?)"));

	public Lazy<Task<PreparedStatement>> DeleteDraftAttachment { get; } = new(() => session.PrepareAsync(
		"DELETE FROM draft_attachments WHERE id = ?"));
}
