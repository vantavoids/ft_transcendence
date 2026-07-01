using Chat.Domain.Results;

namespace Chat.Domain.Messages;

public sealed class DirectMessage
{
	public const int MaxContentLen = 4000;

	private DirectMessage() { }

	private DirectMessage(
		long id,
		long conversationId,
		long senderId,
		long recipientId,
		long? replyToId,
		string? content,
		bool isDeleted,
		DateTimeOffset? editedAt,
		DateTimeOffset createdAt)
	{
		Id = id;
		ConversationId = conversationId;
		SenderId = senderId;
		RecipientId = recipientId;
		ReplyToId = replyToId;
		Content = content;
		IsDeleted = isDeleted;
		EditedAt = editedAt;
		CreatedAt = createdAt;
	}

	public long Id { get; private set; }
	public long ConversationId { get; private set; }
	public long SenderId { get; private set; }
    public long RecipientId { get; private set; }
	public long? ReplyToId { get; private set; }
	public string? Content { get; private set; }
	public bool IsDeleted { get; private set; }
	public DateTimeOffset? EditedAt { get; private set; }
	public DateTimeOffset CreatedAt { get; private set; }

	public static DirectMessage Reconstitute(
		long id,
		long conversationId,
		long senderId,
		long recipientId,
		long? replyToId,
		string? content,
		bool isDeleted,
		DateTimeOffset? editedAt,
		DateTimeOffset createdAt)
		=> new(id, conversationId, senderId, recipientId, replyToId, content, isDeleted, editedAt, createdAt);

	public static Result<DirectMessage> Create(
		long id,
		long conversationId,
		long senderId,
		long recipientId,
		long? replyToId,
		string? content,
		DateTimeOffset now,
		bool hasAttachments = false)
	{
		if (id <= 0)
			return DirectMessageFailures.InvalidId;

		if (conversationId <= 0)
			return DirectMessageFailures.InvalidConversationId;

		if (senderId <= 0)
			return DirectMessageFailures.InvalidSenderId;

		if (recipientId <= 0)
			return DirectMessageFailures.InvalidRecipientId;

		if (senderId == recipientId)
			return DirectMessageFailures.CannotMessageSelf;

		// content is optional only when the message carries at least one attachment
		if (string.IsNullOrWhiteSpace(content))
		{
			if (!hasAttachments)
				return DirectMessageFailures.ContentRequired;
		}
		else if (content.Length > MaxContentLen)
		{
			return DirectMessageFailures.ContentTooLong;
		}

		return new DirectMessage(
			id: id,
			conversationId: conversationId,
			senderId: senderId,
			recipientId: recipientId,
			replyToId: replyToId,
			content: content,
			isDeleted: false,
			editedAt: null,
			createdAt: now);
	}
}
