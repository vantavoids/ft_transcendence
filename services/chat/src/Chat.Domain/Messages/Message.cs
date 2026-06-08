using Chat.Domain.Results;

namespace Chat.Domain.Messages;

public sealed class Message
{
	public const int MaxContentLen = 4000;

	// driver-friendly constructor (nope, it's not just an EF Core thingy 🥀)
	private Message() { }

	private Message(
		long id,
		long channelId,
		long authorId,
		string? content,
		long? replyToId,
		DateTimeOffset? editedAt,
		bool isDeleted,
		DateTimeOffset createdAt)
	{
		Id = id;
		ChannelId = channelId;
		AuthorId = authorId;
		Content = content;
		ReplyToId = replyToId;
		EditedAt = editedAt;
		IsDeleted = isDeleted;
		CreatedAt = createdAt;
	}

	public long Id { get; private set; }
	public long ChannelId { get; private set; }
	public long AuthorId { get; private set; }
	public string? Content { get; private set; }
	public long? ReplyToId { get; private set; }
	public DateTimeOffset? EditedAt { get; private set; }
	public bool IsDeleted { get; private set; }
	public DateTimeOffset CreatedAt { get; private set; }

	public static Message Reconstitute(
		long id,
		long channelId,
		long authorId,
		string? content,
		long? replyToId,
		DateTimeOffset? editedAt,
		bool isDeleted,
		DateTimeOffset createdAt)
		=> new(id, channelId, authorId, content, replyToId, editedAt, isDeleted, createdAt);

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

	public static Result<Message> Create(
		long id,
		long channelId,
		long authorId,
		string? content,
		long? replyToId,
		DateTimeOffset now)
	{
		if (id <= 0)
			return MessageFailures.InvalidId;

		if (channelId <= 0)
			return MessageFailures.InvalidChannelId;

		if (authorId <= 0)
			return MessageFailures.InvalidAuthorId;

		if (string.IsNullOrWhiteSpace(content))
			return MessageFailures.ContentRequired;

		if (content.Length > MaxContentLen)
			return MessageFailures.ContentTooLong;

		return new Message(
			id: id,
			channelId: channelId,
			authorId: authorId,
			content: content,
			replyToId: replyToId,
			editedAt: null,
			isDeleted: false,
			createdAt: now);
	}
}
