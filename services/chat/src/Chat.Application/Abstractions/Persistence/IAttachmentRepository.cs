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

	Task<AttachmentMetadata?> GetChannelAttachmentAsync(long channelId, long messageId, long id, CancellationToken ct);

	Task<IReadOnlyList<AttachmentMetadata>> GetChannelMessageAttachmentsAsync(long channelId, long messageId, CancellationToken ct);

	/// <summary>
	/// hydrates attachments for a whole page of messages in one query, keyed by
	/// message id (messages with no attachments are simply absent from the lookup)
	/// </summary>
	Task<ILookup<long, AttachmentMetadata>> GetChannelMessagesAttachmentsAsync(
		long channelId, IReadOnlyList<long> messageIds, CancellationToken ct);

	Task<AttachmentMetadata?> GetDmAttachmentAsync(long conversationId, long messageId, long id, CancellationToken ct);

	Task<IReadOnlyList<AttachmentMetadata>> GetDmMessageAttachmentsAsync(long conversationId, long messageId, CancellationToken ct);

	/// <summary>
	/// hydrates attachments for a whole page of DM messages in one query, keyed by
	/// message id (messages with no attachments are simply absent from the lookup)
	/// </summary>
	Task<ILookup<long, AttachmentMetadata>> GetDmMessagesAttachmentsAsync(
		long conversationId, IReadOnlyList<long> messageIds, CancellationToken ct);
}

/// <summary>
/// where an attached attachment lives, per <c>attachment_lookup</c>. channel
/// attachments carry <see cref="ChannelId"/>; DM attachments carry
/// <see cref="ConversationId"/>
/// </summary>
public sealed record AttachmentLocation(bool IsDm, long? ChannelId, long? ConversationId, long MessageId);
