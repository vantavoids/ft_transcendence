using Cassandra;
using Chat.Application.Abstractions.Persistence;
using Chat.Domain.Attachments;
using Chat.Domain.Conversations;
using Chat.Domain.Messages;

namespace Chat.Persistence.Repositories;

/// <summary>
/// Channel messages and DMs live in separate Cassandra tables with slightly
/// different columns so every write/read still needs to pick the right statement.
/// </summary>
internal sealed class MessageRepository(ISession session, MessageStatements statements) : IMessageRepository
{
	public async Task AddAsync(Message message, string? nonce, IReadOnlyList<AttachmentMetadata> attachments, CancellationToken ct)
	{
		var createdAt = message.CreatedAt.UtcDateTime;

		var batch = new BatchStatement()
			.SetBatchType(BatchType.Logged)
			.Add(await BindInsertMessageAsync(message, createdAt))
			.Add(await BindInsertLookupAsync(message, createdAt));

		if (message.IsDirectMessage)
			foreach (var upsert in await BindConversationUpsertsAsync(message, createdAt))
				batch.Add(upsert);

		if (nonce is not null)
			batch.Add(await BindInsertNonceAsync(message, nonce));

		if (attachments.Count > 0)
		{
			var deleteDraft = await statements.DeleteDraftAttachment.Value;
			foreach (var attachment in attachments)
			{
				batch.Add(await BindInsertAttachmentAsync(message, attachment));
				batch.Add(await BindInsertAttachmentLookupAsync(message, attachment));
				batch.Add(deleteDraft.Bind(attachment.Id));
			}
		}

		await session.ExecuteAsync(batch);
	}

	/* ### AddAsync conditionnal helpers #### */

	private async Task<BoundStatement> BindInsertMessageAsync(Message message, DateTime createdAt)
	{
		if (message.IsDirectMessage)
		{
			var stmt = await statements.InsertDirectMessage.Value;
			return stmt.Bind(
				message.ContainerId, createdAt, message.Id, message.AuthorId, message.RecipientId!.Value,
				message.Content, message.EditedAt?.UtcDateTime, message.IsDeleted, message.ReplyToId);
		}

		var channelStmt = await statements.InsertChannelMessage.Value;
		return channelStmt.Bind(
			message.ContainerId, createdAt, message.Id, message.AuthorId,
			message.Content, message.EditedAt?.UtcDateTime, message.IsDeleted, message.ReplyToId);
	}

	private async Task<BoundStatement> BindInsertLookupAsync(Message message, DateTime createdAt)
	{
		var stmt = await statements.InsertLookup.Value;
		return message.IsDirectMessage
			? stmt.Bind(message.Id, true, (long?)null, message.ContainerId, createdAt, message.AuthorId)
			: stmt.Bind(message.Id, false, message.ContainerId, (long?)null, createdAt, message.AuthorId);
	}

	private async Task<BoundStatement[]> BindConversationUpsertsAsync(Message message, DateTime createdAt)
	{
		var recipientId = message.RecipientId!.Value;
		var preview = BuildPreview(message.Content);
		var stmt = await statements.UpsertConversation.Value;

		// one row per direction, so both participants' conversation lists see the update
		return
		[
			stmt.Bind(message.AuthorId, recipientId, message.ContainerId, createdAt, preview),
			stmt.Bind(recipientId, message.AuthorId, message.ContainerId, createdAt, preview),
		];
	}

	private async Task<BoundStatement> BindInsertNonceAsync(Message message, string nonce)
	{
		var stmt = await (message.IsDirectMessage ? statements.InsertDmNonce : statements.InsertChannelNonce).Value;
		var secondKey = message.IsDirectMessage ? message.RecipientId!.Value : message.ContainerId;

		return stmt.Bind(message.AuthorId, secondKey, nonce, message.Id);
	}

	private async Task<BoundStatement> BindInsertAttachmentAsync(Message message, AttachmentMetadata attachment)
	{
		var stmt = await (message.IsDirectMessage ? statements.InsertDmAttachment : statements.InsertMessageAttachment).Value;
		return stmt.Bind(
			message.ContainerId, message.Id, attachment.Id,
			attachment.Url, attachment.Filename, attachment.SizeBytes, attachment.MimeType);
	}

	private async Task<BoundStatement> BindInsertAttachmentLookupAsync(Message message, AttachmentMetadata attachment)
	{
		var stmt = await statements.InsertAttachmentLookup.Value;
		return message.IsDirectMessage
			? stmt.Bind(attachment.Id, true, (long?)null, message.ContainerId, message.Id)
			: stmt.Bind(attachment.Id, false, message.ContainerId, (long?)null, message.Id);
	}

	/* ### Find by nonce #### */

	public Task<long?> FindChannelNonceAsync(long authorId, long channelId, string nonce, CancellationToken ct) =>
		FindNonceAsync(statements.FindChannelNonce, authorId, channelId, nonce);

	public Task<long?> FindDmNonceAsync(long senderId, long recipientId, string nonce, CancellationToken ct) =>
		FindNonceAsync(statements.FindDmNonce, senderId, recipientId, nonce);

	private async Task<long?> FindNonceAsync(Lazy<Task<PreparedStatement>> statementSource, long first, long second, string nonce)
	{
		var stmt = await statementSource.Value;
		var row = (await session.ExecuteAsync(stmt.Bind(first, second, nonce))).FirstOrDefault();
		return row?.GetValue<long>("message_id");
	}

	/* ### Mutation #### */

	public async Task UpdateContentAsync(Message message, CancellationToken ct)
	{
		var stmt = await (message.IsDirectMessage
			? statements.UpdateDirectMessageContent
			: statements.UpdateChannelMessageContent).Value;

		await session.ExecuteAsync(stmt.Bind(
			message.Content, message.EditedAt!.Value.UtcDateTime,
			message.ContainerId, message.CreatedAt.UtcDateTime,
			message.Id));
	}

	public async Task SoftDeleteAsync(Message message, CancellationToken ct)
	{
		var stmt = await (message.IsDirectMessage
			? statements.SoftDeleteDirectMessage
			: statements.SoftDeleteChannelMessage).Value;

		await session.ExecuteAsync(stmt.Bind(message.ContainerId, message.CreatedAt.UtcDateTime, message.Id));
	}

	/* ### Messages retrieval #### */

	public async Task<IReadOnlyList<Message>> GetChannelMessagesAsync(long channelId, DateTimeOffset beforeTime, int limit, CancellationToken ct)
	{
		var stmt = await statements.SelectChannelMessagesRange.Value;
		var rows = await session.ExecuteAsync(stmt.Bind(channelId, beforeTime.UtcDateTime, limit));

		// unlike DM history, channel history hides soft-deleted messages entirely
		return rows.Select(ReconstituteChannelMessage).Where(m => !m.IsDeleted).ToList();
	}

	public async Task<IReadOnlyList<Message>> GetDirectMessagesAsync(long conversationId, DateTimeOffset beforeTime, int limit, CancellationToken ct)
	{
		var stmt = await statements.SelectDirectMessagesByConversation.Value;
		var rows = await session.ExecuteAsync(stmt.Bind(conversationId, beforeTime.UtcDateTime, limit));

		return rows.Select(ReconstituteDirectMessage).Where(dm => !dm.IsDeleted).ToList();
	}

	public async Task<Message?> GetByIdAsync(long messageId, CancellationToken ct)
	{
		var lookup = await GetLookupAsync(messageId);
		if (lookup is null)
			return null;

		var (stmt, reconstitute) = lookup.Value.IsDm
			? (await statements.SelectDirectMessage.Value, (Func<Row, Message>)ReconstituteDirectMessage)
			: (await statements.SelectChannelMessage.Value, ReconstituteChannelMessage);

		var row = (await session.ExecuteAsync(stmt.Bind(lookup.Value.ContainerId, lookup.Value.CreatedAt, messageId))).FirstOrDefault();
		return row is null ? null : reconstitute(row);
	}

	/* ### Retrieving helpers #### */

	private static Message ReconstituteChannelMessage(Row row) => Message.Reconstitute(
		id: row.GetValue<long>("id"),
		containerId: row.GetValue<long>("channel_id"),
		authorId: row.GetValue<long>("author_id"),
		recipientId: null,
		content: row.GetValue<string>("content"),
		replyToId: row.GetValue<long?>("reply_to_id"),
		editedAt: row.GetValue<DateTime?>("edited_at"),
		isDeleted: row.GetValue<bool>("is_deleted"),
		createdAt: new DateTimeOffset(row.GetValue<DateTime>("created_at"), TimeSpan.Zero));

	private static Message ReconstituteDirectMessage(Row row) => Message.Reconstitute(
		id: row.GetValue<long>("id"),
		containerId: row.GetValue<long>("conversation_id"),
		authorId: row.GetValue<long>("sender_id"),
		recipientId: row.GetValue<long>("recipient_id"),
		content: row.GetValue<string?>("content"),
		replyToId: row.GetValue<long?>("reply_to_id"),
		editedAt: row.GetValue<DateTime?>("edited_at"),
		isDeleted: row.GetValue<bool>("is_deleted"),
		createdAt: new DateTimeOffset(row.GetValue<DateTime>("created_at"), TimeSpan.Zero));

	private async Task<MessageLookup?> GetLookupAsync(long messageId)
	{
		var selectLookup = await statements.SelectLookup.Value;
		var row = (await session.ExecuteAsync(selectLookup.Bind(messageId))).FirstOrDefault();

		if (row is null)
			return null;

		var isDm = row.GetValue<bool>("is_dm");
		var createdAt = row.GetValue<DateTime?>("created_at");
		var containerId = row.GetValue<long?>(isDm ? "conversation_id" : "channel_id");

		if (containerId is null || createdAt is null)
			return null;

		return new MessageLookup(isDm, containerId.Value, createdAt.Value);
	}

	/* ### DM Conversations #### */

	public async Task<long?> FindConversationAsync(long senderId, long recipientId, CancellationToken ct)
	{
		var stmt = await statements.FindConversation.Value;
		var row = (await session.ExecuteAsync(stmt.Bind(senderId, recipientId))).FirstOrDefault();

		return row?.GetValue<long>("conversation_id");
	}

	public async Task<long> GetOrCreateConversationAsync(long senderId, long recipientId, long candidateId, CancellationToken ct)
	{
		// Canonicalize on (min, max) so that A->B and B->A races contend on the
		// same user_conversations partition/clustering key
		var userId = Math.Min(senderId, recipientId);
		var partnerId = Math.Max(senderId, recipientId);

		var stmt = await statements.InsertConversationIfNotExists.Value;
		var row = (await session.ExecuteAsync(stmt.Bind(userId, partnerId, candidateId))).FirstOrDefault();

		// A response row only carries "[applied]" on success
		if (row is null || row.GetValue<bool>("[applied]"))
			return candidateId;

		return row.GetValue<long>("conversation_id");
	}

	public async Task DeleteConversationAsync(long userId, CancellationToken ct)
	{
		var stmt = await statements.DeleteConversation.Value;
		await session.ExecuteAsync(stmt.Bind(userId));
	}

	public async Task<IReadOnlyList<DmConversation>> GetConversationsAsync(long userId, CancellationToken ct)
	{
		var stmt = await statements.SelectConversationsByUser.Value;
		var rows = await session.ExecuteAsync(stmt.Bind(userId));

		// last_message_at can be null between GetOrCreateConversationAsync
		// creating the row and the first message's AddAsync populating it; such
		// a conversation has no message to preview yet, so it is not listed
		return rows
			.Where(row => row.GetValue<DateTime?>("last_message_at") is not null)
			.Select(row => new DmConversation(
				PartnerId: row.GetValue<long>("partner_id"),
				LastMessageAt: new DateTimeOffset(row.GetValue<DateTime>("last_message_at"), TimeSpan.Zero),
				LastPreview: row.GetValue<string?>("last_preview"),
				IsArchived: row.GetValue<bool>("is_archived")))
			.ToList();
	}

	private static string? BuildPreview(string? content)
	{
		if (string.IsNullOrWhiteSpace(content))
			return null;

		var trimmed = content.Trim();
		return trimmed.Length <= 100 ? trimmed : trimmed[..100];
	}

	private readonly record struct MessageLookup(bool IsDm, long ContainerId, DateTime CreatedAt);
}
