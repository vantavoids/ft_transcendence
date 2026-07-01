using Cassandra;
using Chat.Application.Abstractions.Persistence;
using Chat.Domain.Messages;

namespace Chat.Persistence.Repositories;

internal sealed class DirectMessageRepository(
	ISession session,
	DirectMessageStatements statements)
	: IDirectMessageRepository
{
	public async Task<long?> FindConversationAsync(long senderId, long recipientId, CancellationToken ct)
	{
		var stmt = await statements.FindConversation.Value;
		var row = (await session.ExecuteAsync(stmt.Bind(senderId, recipientId))).FirstOrDefault();

		return row?.GetValue<long>("conversation_id");
	}

	public async Task<long?> FindReplyExistsAsync(long conversationId, long replyToId, CancellationToken ct)
	{
		var message = await GetByIdAsync(replyToId, ct);
		if (message is null || message.ConversationId != conversationId || message.IsDeleted)
			return null;

		return replyToId;
	}

	public async Task<long> GetOrCreateConversationAsync(long senderId, long recipientId, long candidateId, CancellationToken ct)
	{
		// Canonicalize on (min, max) so that A->B and B->A races contend on the
		// same user_conversations partition/clustering key
		var userId = Math.Min(senderId, recipientId);
		var partnerId = Math.Max(senderId, recipientId);

		var stmt = await statements.InsertConversationIfNotExists.Value;
		var row = (await session.ExecuteAsync(stmt.Bind(
														userId,
														partnerId,
														candidateId
													))).FirstOrDefault();

		// A CAS response row only carries "[applied]" on success
		if (row is null || row.GetValue<bool>("[applied]"))
			return candidateId;

		return row.GetValue<long>("conversation_id");
	}

	public async Task AddAsync(DirectMessage message, string? nonce, CancellationToken ct)
	{
		var insertDirectMessage = await statements.InsertDirectMessage.Value;
		var insertLookup = await statements.InsertLookup.Value;
		var upsertConversation = await statements.UpsertConversation.Value;

		var createdAt = message.CreatedAt.UtcDateTime;
		var editedAt = message.EditedAt?.UtcDateTime;
		var preview = BuildPreview(message.Content);

		var batch = new BatchStatement()
			.SetBatchType(BatchType.Logged)
			.Add(insertDirectMessage.Bind(
				message.ConversationId,
				createdAt,
				message.Id,
				message.SenderId,
				message.RecipientId,
				message.Content,
				editedAt,
				message.IsDeleted,
				message.ReplyToId))
			.Add(insertLookup.Bind(
				message.Id,
				(long?)null,
				message.ConversationId,
				createdAt,
				message.SenderId))
			.Add(upsertConversation.Bind(
				message.SenderId,
				message.RecipientId,
				message.ConversationId,
				createdAt,
				preview))
			.Add(upsertConversation.Bind(
				message.RecipientId,
				message.SenderId,
				message.ConversationId,
				createdAt,
				preview));

		if (nonce is not null)
		{
			var insertNonce = await statements.InsertNonce.Value;
			batch.Add(insertNonce.Bind(
				message.SenderId,
				message.RecipientId,
				nonce,
				message.Id));
		}

		await session.ExecuteAsync(batch);
	}

	public async Task<long?> FindNonceAsync(long senderId, long recipientId, string nonce, CancellationToken ct)
	{
		var stmt = await statements.FindNonce.Value;
		var row = (await session.ExecuteAsync(stmt.Bind(senderId, recipientId, nonce))).FirstOrDefault();

		return row?.GetValue<long>("message_id");
	}

	public async Task<DirectMessage?> GetByIdAsync(long messageId, CancellationToken ct)
	{
		var lookup = await GetLookupAsync(messageId);
		if (lookup is null)
			return null;

		var selectMessage = await statements.SelectDirectMessage.Value;
		var row = (await session.ExecuteAsync(selectMessage.Bind(
			lookup.Value.ConversationId,
			lookup.Value.CreatedAt,
			messageId))).FirstOrDefault();

		if (row is null)
			return null;

		return DirectMessage.Reconstitute(
			id: row.GetValue<long>("id"),
			conversationId: row.GetValue<long>("conversation_id"),
			senderId: row.GetValue<long>("sender_id"),
			recipientId: row.GetValue<long>("recipient_id"),
			replyToId: row.GetValue<long?>("reply_to_id"),
			content: row.GetValue<string?>("content"),
			isDeleted: row.GetValue<bool>("is_deleted"),
			editedAt: row.GetValue<DateTime?>("edited_at"),
			createdAt: new DateTimeOffset(row.GetValue<DateTime>("created_at"), TimeSpan.Zero));
	}

	private async Task<MessageLookup?> GetLookupAsync(long messageId)
	{
		var selectLookup = await statements.SelectLookup.Value;
		var row = (await session.ExecuteAsync(selectLookup.Bind(messageId))).FirstOrDefault();

		if (row is null)
			return null;

		var isDm = row.GetValue<bool>("is_dm");
		if (!isDm)
			return null;

		var conversationId = row.GetValue<long?>("conversation_id");
		var createdAt = row.GetValue<DateTime?>("created_at");

		if (conversationId is null || createdAt is null)
			return null;

		return new MessageLookup(conversationId.Value, createdAt.Value);
	}

	private static string? BuildPreview(string? content)
	{
		if (string.IsNullOrWhiteSpace(content))
			return null;

		var trimmed = content.Trim();
		return trimmed.Length <= 100 ? trimmed : trimmed[..100];
	}

	public async Task<IReadOnlyList<DirectMessage>> ListAsync(
			long conversationId,
			int limit,
			CancellationToken ct)
	{
		var stmt = await statements.SelectDirectMessagesByConversation.Value;
		var rows = await session.ExecuteAsync(stmt.Bind(conversationId, limit));

		return rows
			.Select(row => DirectMessage.Reconstitute(
						id: row.GetValue<long>("id"),
						conversationId: row.GetValue<long>("conversation_id"),
						senderId: row.GetValue<long>("sender_id"),
						recipientId: row.GetValue<long>("recipient_id"),
						replyToId: row.GetValue<long?>("reply_to_id"),
						content: row.GetValue<string?>("content"),
						isDeleted: row.GetValue<bool>("is_deleted"),
						editedAt: row.GetValue<DateTime?>("edited_at"),
						createdAt: new DateTimeOffset(row.GetValue<DateTime>("created_at"), TimeSpan.Zero)))
			.ToList();
	}

	private readonly record struct MessageLookup(long ConversationId, DateTime CreatedAt);
}
