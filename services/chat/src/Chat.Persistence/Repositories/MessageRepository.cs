using Cassandra;
using Chat.Application.Abstractions.Persistence;
using Chat.Domain.Messages;

namespace Chat.Persistence.Repositories;

internal sealed class MessageRepository(ISession session) : IMessageRepository
{
	// statements are prepared lazily once per process and reused. AsyncLazy via
	// Task<PreparedStatement> means concurrent first-callers all await the same
	// in-flight prepare instead of racing the driver
	private readonly Lazy<Task<PreparedStatement>> _insertMessage = new(() => session.PrepareAsync(
		"INSERT INTO messages " +
		"(channel_id, created_at, id, author_id, content, edited_at, is_deleted, reply_to_id) " +
		"VALUES (?, ?, ?, ?, ?, ?, ?, ?)"));

	private readonly Lazy<Task<PreparedStatement>> _insertLookup = new(() => session.PrepareAsync(
		"INSERT INTO message_lookup " +
		"(message_id, is_dm, channel_id, conversation_id, created_at, author_id) " +
		"VALUES (?, false, ?, ?, ?, ?)"));

	public async Task AddAsync(Message message, CancellationToken ct)
	{
		var insertMessage = await _insertMessage.Value;
		var insertLookup = await _insertLookup.Value;

		var createdAt = message.CreatedAt.UtcDateTime;
		var editedAt = message.EditedAt?.UtcDateTime;

		var messageStmt = insertMessage.Bind(
			message.ChannelId,
			createdAt,
			message.Id,
			message.AuthorId,
			message.Content,
			editedAt,
			message.IsDeleted,
			message.ReplyToId);

		var lookupStmt = insertLookup.Bind(
			message.Id,
			message.ChannelId,
			// conversation_id is bound NULL: regular CQL INSERTs accept null
			// binds for non-key columns, which is what we want for channel
			// messages (this row is the channel-vs-DM discriminator)
			(long?)null,
			createdAt,
			message.AuthorId);

		var batch = new BatchStatement()
			.SetBatchType(BatchType.Logged)
			.Add(messageStmt)
			.Add(lookupStmt);

		await session.ExecuteAsync(batch);
	}
}
