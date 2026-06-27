using Cassandra;
using Chat.Application.Abstractions.Persistence;
using Chat.Domain.Attachments;

namespace Chat.Persistence.Repositories;

internal sealed class AttachmentRepository(ISession session, AttachmentStatements statements) : IAttachmentRepository
{
	public async Task AddDraftAsync(DraftAttachment draft, CancellationToken ct)
	{
		var stmt = await statements.InsertDraft.Value;
		await session.ExecuteAsync(stmt.Bind(
			draft.Id,
			draft.UploaderId,
			draft.Url,
			draft.Filename,
			draft.SizeBytes,
			draft.MimeType,
			draft.CreatedAt.UtcDateTime));
	}

	public async Task<DraftAttachment?> GetDraftAsync(long id, CancellationToken ct)
	{
		var stmt = await statements.SelectDraft.Value;
		var row = (await session.ExecuteAsync(stmt.Bind(id))).FirstOrDefault();
		if (row is null)
			return null;

		return DraftAttachment.Reconstitute(
			id: row.GetValue<long>("id"),
			uploaderId: row.GetValue<long>("uploader_id"),
			url: row.GetValue<string>("url"),
			filename: row.GetValue<string>("filename"),
			sizeBytes: row.GetValue<long>("size_bytes"),
			mimeType: row.GetValue<string>("mime_type"),
			createdAt: new DateTimeOffset(row.GetValue<DateTime>("created_at"), TimeSpan.Zero));
	}

	public async Task<bool> IsAttachedAsync(long id, CancellationToken ct)
	{
		var stmt = await statements.SelectLookup.Value;
		var row = (await session.ExecuteAsync(stmt.Bind(id))).FirstOrDefault();
		return row is not null;
	}

	public async Task<AttachmentLocation?> GetLocationAsync(long id, CancellationToken ct)
	{
		var stmt = await statements.SelectLookup.Value;
		var row = (await session.ExecuteAsync(stmt.Bind(id))).FirstOrDefault();
		if (row is null)
			return null;

		return new AttachmentLocation(
			IsDm: row.GetValue<bool>("is_dm"),
			ChannelId: row.GetValue<long?>("channel_id"),
			ConversationId: row.GetValue<long?>("conversation_id"),
			MessageId: row.GetValue<long>("message_id"));
	}

	public async Task<AttachmentMetadata?> GetChannelAttachmentAsync(long channelId, long messageId, long id, CancellationToken ct)
	{
		var stmt = await statements.SelectChannelAttachment.Value;
		var row = (await session.ExecuteAsync(stmt.Bind(channelId, messageId, id))).FirstOrDefault();
		return row is null ? null : MapMetadata(row);
	}

	public async Task<IReadOnlyList<AttachmentMetadata>> GetChannelMessageAttachmentsAsync(long channelId, long messageId, CancellationToken ct)
	{
		var stmt = await statements.SelectChannelMessageAttachments.Value;
		var rows = await session.ExecuteAsync(stmt.Bind(channelId, messageId));
		return rows.Select(MapMetadata).ToList();
	}

	public async Task<ILookup<long, AttachmentMetadata>> GetChannelMessagesAttachmentsAsync(
		long channelId, IReadOnlyList<long> messageIds, CancellationToken ct)
	{
		if (messageIds.Count == 0)
			return Enumerable.Empty<AttachmentMetadata>().ToLookup(_ => 0L);

		var stmt = await statements.SelectChannelMessagesAttachments.Value;
		var rows = await session.ExecuteAsync(stmt.Bind(channelId, messageIds.ToArray()));

		return rows.ToLookup(row => row.GetValue<long>("message_id"), MapMetadata);
	}

	private static AttachmentMetadata MapMetadata(Row row) => new(
		Id: row.GetValue<long>("id"),
		Url: row.GetValue<string>("url"),
		Filename: row.GetValue<string>("filename"),
		SizeBytes: row.GetValue<long>("size_bytes"),
		MimeType: row.GetValue<string>("mime_type"));
}
