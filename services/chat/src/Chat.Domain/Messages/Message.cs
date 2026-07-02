using Chat.Domain.Results;

namespace Chat.Domain.Messages;

/// <summary>
/// A single message, either posted to a channel or sent as a direct message.
/// <see cref="ContainerId"/> is the channel id or the conversation id
/// depending on context; <see cref="RecipientId"/> is the sole discriminator
/// (set only for DMs), mirroring the <c>is_dm</c> routing flag already used
/// by <c>message_lookup</c> and <c>attachment_lookup</c>.
/// </summary>
public sealed class Message
{
	public const int MaxContentLen = 4000;

	// driver-friendly constructor (nope, it's not just an EF Core thingy 🥀)
	private Message() { }

	private Message(
		long id,
		long containerId,
		long authorId,
		long? recipientId,
		string? content,
		long? replyToId,
		DateTimeOffset? editedAt,
		bool isDeleted,
		DateTimeOffset createdAt)
	{
		Id = id;
		ContainerId = containerId;
		AuthorId = authorId;
		RecipientId = recipientId;
		Content = content;
		ReplyToId = replyToId;
		EditedAt = editedAt;
		IsDeleted = isDeleted;
		CreatedAt = createdAt;
	}

	public long Id { get; private set; }

	/// <summary>the channel id for a channel message, the conversation id for a DM</summary>
	public long ContainerId { get; private set; }

	/// <summary>author for a channel message, sender for a DM</summary>
	public long AuthorId { get; private set; }

	/// <summary>DM-only: the other participant</summary>
	public long? RecipientId { get; private set; }

	public string? Content { get; private set; }
	public long? ReplyToId { get; private set; }
	public DateTimeOffset? EditedAt { get; private set; }
	public bool IsDeleted { get; private set; }
	public DateTimeOffset CreatedAt { get; private set; }

	public bool IsDirectMessage => RecipientId is not null;

	public static Message Reconstitute(
		long id,
		long containerId,
		long authorId,
		long? recipientId,
		string? content,
		long? replyToId,
		DateTimeOffset? editedAt,
		bool isDeleted,
		DateTimeOffset createdAt)
		=> new(id, containerId, authorId, recipientId, content, replyToId, editedAt, isDeleted, createdAt);

	public Result<Message> Edit(string? content, DateTimeOffset now)
	{
		if (IsDeleted)
			return MessageFailures.AlreadyDeleted;

		if (string.IsNullOrWhiteSpace(content))
			return MessageFailures.ContentRequired;

		if (content.Length > MaxContentLen)
			return MessageFailures.ContentTooLong;

		Content = content;
		EditedAt = now;
		return this;
	}

	public Result<Message> Delete()
	{
		if (IsDeleted)
			return MessageFailures.AlreadyDeleted;

		IsDeleted = true;
		return this;
	}

	public static Result<Message> CreateForChannel(
		long id,
		long channelId,
		long authorId,
		string? content,
		long? replyToId,
		DateTimeOffset now,
		bool hasAttachments = false)
	{
		if (id <= 0)
			return MessageFailures.InvalidId;

		if (channelId <= 0)
			return MessageFailures.InvalidChannelId;

		if (authorId <= 0)
			return MessageFailures.InvalidAuthorId;

		// content is optional only when the message carries at least one attachment
		if (string.IsNullOrWhiteSpace(content))
		{
			if (!hasAttachments)
				return MessageFailures.ContentRequired;
		}
		else if (content.Length > MaxContentLen)
		{
			return MessageFailures.ContentTooLong;
		}

		return new Message(
			id: id,
			containerId: channelId,
			authorId: authorId,
			recipientId: null,
			content: string.IsNullOrWhiteSpace(content) ? null : content,
			replyToId: replyToId,
			editedAt: null,
			isDeleted: false,
			createdAt: now);
	}

	public static Result<Message> CreateForDirectMessage(
		long id,
		long conversationId,
		long senderId,
		long recipientId,
		string? content,
		long? replyToId,
		DateTimeOffset now,
		bool hasAttachments = false)
	{
		if (id <= 0)
			return MessageFailures.InvalidId;

		if (conversationId <= 0)
			return MessageFailures.InvalidConversationId;

		if (senderId <= 0)
			return MessageFailures.InvalidAuthorId;

		if (recipientId <= 0)
			return MessageFailures.InvalidRecipientId;

		if (senderId == recipientId)
			return MessageFailures.CannotMessageSelf;

		// content is optional only when the message carries at least one attachment
		if (string.IsNullOrWhiteSpace(content))
		{
			if (!hasAttachments)
				return MessageFailures.ContentRequired;
		}
		else if (content.Length > MaxContentLen)
		{
			return MessageFailures.ContentTooLong;
		}

		return new Message(
			id: id,
			containerId: conversationId,
			authorId: senderId,
			recipientId: recipientId,
			content: string.IsNullOrWhiteSpace(content) ? null : content,
			replyToId: replyToId,
			editedAt: null,
			isDeleted: false,
			createdAt: now);
	}
}
