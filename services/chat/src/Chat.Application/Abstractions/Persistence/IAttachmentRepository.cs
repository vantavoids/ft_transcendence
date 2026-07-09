using Chat.Domain.Attachments;

namespace Chat.Application.Abstractions.Persistence;

public interface IAttachmentRepository
{
	Task AddDraftAsync(DraftAttachment draft, CancellationToken ct);

	Task<DraftAttachment?> GetDraftAsync(long id, CancellationToken ct);

	/// <summary>true if the attachment is already bound to a sent message</summary>
	Task<bool> IsAttachedAsync(long id, CancellationToken ct);

	/// <summary>resolves an attachment id to its owning message for the download path</summary>
	Task<AttachmentLocation?> GetLocationAsync(long id, CancellationToken ct);

	/// <summary>point lookup for one attachment on one message</summary>
	Task<AttachmentMetadata?> GetAttachmentAsync(long containerId, bool isDm, long messageId, long id, CancellationToken ct);

	/// <summary>all attachments on one message</summary>
	Task<IReadOnlyList<AttachmentMetadata>> GetMessageAttachmentsAsync(long containerId, bool isDm, long messageId, CancellationToken ct);

	/// <summary>
	/// hydrates attachments for a whole page of messages in one query, keyed by
	/// message id (messages with no attachments are simply absent from the lookup)
	/// </summary>
	Task<ILookup<long, AttachmentMetadata>> GetMessagesAttachmentsAsync(
		long containerId, bool isDm, IReadOnlyList<long> messageIds, CancellationToken ct);
}

/// <summary>
/// where an attached attachment lives, per <c>attachment_lookup</c>.
/// <see cref="ContainerId"/> is the channel id or the conversation id depending
/// on <see cref="IsDm"/>.
/// </summary>
public sealed record AttachmentLocation(bool IsDm, long ContainerId, long MessageId);
