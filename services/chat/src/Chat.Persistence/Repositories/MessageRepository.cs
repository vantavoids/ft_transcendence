using Cassandra;
using Chat.Application.Abstractions.Persistence;
using Chat.Domain.Messages;

namespace Chat.Persistence.Repositories;

internal sealed class MessageRepository(ISession session, MessageStatements statements) : IMessageRepository
{
	public async Task AddAsync(Message message, string? nonce, CancellationToken ct)
	{
		var insertMessage = await statements.InsertMessage.Value;
		var insertLookup = await statements.InsertLookup.Value;

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
			var insertNonce = await statements.InsertNonce.Value;
			batch.Add(insertNonce.Bind(message.AuthorId, message.ChannelId, nonce, message.Id));
		}

		await session.ExecuteAsync(batch);
	}

	public async Task<long?> FindNonceAsync(long authorId, long channelId, string nonce, CancellationToken ct)
	{
		var stmt = await statements.FindNonce.Value;
		var row = await session.ExecuteAsync(stmt.Bind(authorId, channelId, nonce));
		var first = row.FirstOrDefault();
		return first?.GetValue<long>("message_id");
	}

	public async Task<Message?> GetByIdAsync(long messageId, CancellationToken ct)
	{
		var selectLookup = await statements.SelectLookup.Value;
		var lookupRow = (await session.ExecuteAsync(selectLookup.Bind(messageId))).FirstOrDefault();
		if (lookupRow is null)
			return null;

		var channelId = lookupRow.GetValue<long>("channel_id");
		var createdAt = lookupRow.GetValue<DateTime>("created_at");

		var selectMessage = await statements.SelectMessage.Value;
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
