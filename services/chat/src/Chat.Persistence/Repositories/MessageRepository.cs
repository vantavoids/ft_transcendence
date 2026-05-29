using Cassandra;
using Chat.Application.Abstractions.Persistence;
using Chat.Domain.Messages;

namespace Chat.Persistence.Repositories;

internal sealed class MessageRepository(ISession session) : IMessageRepository
{
	private readonly Lazy<Task<PreparedStatement>> _insertMessage = new(() => session.PrepareAsync(
		"INSERT INTO messages " +
		"(channel_id, created_at, id, author_id, content, edited_at, is_deleted, reply_to_id) " +
		"VALUES (?, ?, ?, ?, ?, ?, ?, ?)"));

	private readonly Lazy<Task<PreparedStatement>> _insertLookup = new(() => session.PrepareAsync(
		"INSERT INTO message_lookup " +
		"(message_id, is_dm, channel_id, conversation_id, created_at, author_id) " +
		"VALUES (?, false, ?, ?, ?, ?)"));

	private readonly Lazy<Task<PreparedStatement>> _insertNonce = new(() => session.PrepareAsync(
		"INSERT INTO message_nonce_dedup (author_id, channel_id, nonce, message_id) VALUES (?, ?, ?, ?)"));

	private readonly Lazy<Task<PreparedStatement>> _findNonce = new(() => session.PrepareAsync(
		"SELECT message_id FROM message_nonce_dedup WHERE author_id = ? AND channel_id = ? AND nonce = ?"));

	private readonly Lazy<Task<PreparedStatement>> _selectLookup = new(() => session.PrepareAsync(
		"SELECT channel_id, created_at FROM message_lookup WHERE message_id = ?"));

	private readonly Lazy<Task<PreparedStatement>> _selectMessage = new(() => session.PrepareAsync(
		"SELECT channel_id, created_at, id, author_id, content, edited_at, is_deleted, reply_to_id " +
		"FROM messages WHERE channel_id = ? AND created_at = ? AND id = ?"));

	public async Task AddAsync(Message message, string? nonce, CancellationToken ct)
	{
		var insertMessage = await _insertMessage.Value;
		var insertLookup = await _insertLookup.Value;

		var createdAt = message.CreatedAt.UtcDateTime;
		var editedAt = message.EditedAt?.UtcDateTime;

		var batch = new BatchStatement()
			.SetBatchType(BatchType.Logged)
			.Add(insertMessage.Bind(
				message.ChannelId,
				createdAt,
				message.Id,
				message.AuthorId,
				message.Content,
				editedAt,
				message.IsDeleted,
				message.ReplyToId))
			.Add(insertLookup.Bind(
				message.Id,
				message.ChannelId,
				(long?)null,
				createdAt,
				message.AuthorId));

		if (nonce is not null)
		{
			var insertNonce = await _insertNonce.Value;
			batch.Add(insertNonce.Bind(message.AuthorId, message.ChannelId, nonce, message.Id));
		}

		await session.ExecuteAsync(batch);
	}

	public async Task<long?> FindNonceAsync(long authorId, long channelId, string nonce, CancellationToken ct)
	{
		var stmt = await _findNonce.Value;
		var row = await session.ExecuteAsync(stmt.Bind(authorId, channelId, nonce));
		var first = row.FirstOrDefault();
		return first?.GetValue<long>("message_id");
	}

	public async Task<Message?> GetByIdAsync(long messageId, CancellationToken ct)
	{
		var selectLookup = await _selectLookup.Value;
		var lookupRow = (await session.ExecuteAsync(selectLookup.Bind(messageId))).FirstOrDefault();
		if (lookupRow is null)
			return null;

		var channelId = lookupRow.GetValue<long>("channel_id");
		var createdAt = lookupRow.GetValue<DateTime>("created_at");

		var selectMessage = await _selectMessage.Value;
		var msgRow = (await session.ExecuteAsync(selectMessage.Bind(channelId, createdAt, messageId))).FirstOrDefault();
		if (msgRow is null)
			return null;

		return Message.Reconstitute(
			id: msgRow.GetValue<long>("id"),
			channelId: msgRow.GetValue<long>("channel_id"),
			authorId: msgRow.GetValue<long>("author_id"),
			content: msgRow.GetValue<string>("content"),
			replyToId: msgRow.GetValue<long?>("reply_to_id"),
			editedAt: msgRow.GetValue<DateTime?>("edited_at"),
			isDeleted: msgRow.GetValue<bool>("is_deleted"),
			createdAt: new DateTimeOffset(createdAt, TimeSpan.Zero));
	}
}
