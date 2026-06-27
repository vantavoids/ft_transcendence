using Cassandra;

namespace Chat.Persistence.Repositories;

internal sealed class AttachmentStatements(ISession session)
{
	// draft_attachments carries default_time_to_live = 3600, so the row auto-expires
	// an hour after this insert unless it gets attached to a message first
	public Lazy<Task<PreparedStatement>> InsertDraft { get; } = new(() => session.PrepareAsync(
		"INSERT INTO draft_attachments " +
		"(id, uploader_id, url, filename, size_bytes, mime_type, created_at) " +
		"VALUES (?, ?, ?, ?, ?, ?, ?)"));

	public Lazy<Task<PreparedStatement>> SelectDraft { get; } = new(() => session.PrepareAsync(
		"SELECT id, uploader_id, url, filename, size_bytes, mime_type, created_at " +
		"FROM draft_attachments WHERE id = ?"));

	public Lazy<Task<PreparedStatement>> SelectLookup { get; } = new(() => session.PrepareAsync(
		"SELECT is_dm, channel_id, conversation_id, message_id " +
		"FROM attachment_lookup WHERE attachment_id = ?"));

	public Lazy<Task<PreparedStatement>> SelectChannelAttachment { get; } = new(() => session.PrepareAsync(
		"SELECT id, url, filename, size_bytes, mime_type " +
		"FROM message_attachments WHERE channel_id = ? AND message_id = ? AND id = ?"));

	public Lazy<Task<PreparedStatement>> SelectChannelMessageAttachments { get; } = new(() => session.PrepareAsync(
		"SELECT id, url, filename, size_bytes, mime_type " +
		"FROM message_attachments WHERE channel_id = ? AND message_id = ?"));

	// IN on message_id (the trailing partition-key column) collapses a page's worth
	// of per-message reads into one query; channel_id pins the leading component.
	// the page limit bounds the IN list, so coordinator fan-out stays small
	public Lazy<Task<PreparedStatement>> SelectChannelMessagesAttachments { get; } = new(() => session.PrepareAsync(
		"SELECT message_id, id, url, filename, size_bytes, mime_type " +
		"FROM message_attachments WHERE channel_id = ? AND message_id IN ?"));
}
