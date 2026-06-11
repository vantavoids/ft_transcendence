using Chat.Domain.Messages;

namespace Chat.Domain.Results;

public static class DirectMessageFailures
{
	public static readonly Failure ContentRequired =
		new("DirectMessage.ContentRequired", "Direct message content is required.");

	public static readonly Failure ContentTooLong =
		new("DirectMessage.ContentTooLong",
			$"Direct message content must be {DirectMessage.MaxContentLen} characters or fewer.");

	public static readonly Failure InvalidId =
		new("DirectMessage.InvalidId", "Direct message id must be a positive snowflake.");

	public static readonly Failure InvalidConversationId =
		new("DirectMessage.InvalidConversationId", "Conversation id must be a positive snowflake.");

	public static readonly Failure ConversationNotFound =
		new("DirectMessage.ConversationNotFound", "Conversation not found.");

	public static readonly Failure InvalidSenderId =
		new("DirectMessage.InvalidSenderId", "Sender id must be a positive snowflake.");

	public static readonly Failure InvalidRecipientId =
		new("DirectMessage.InvalidRecipientId", "Recipient id must be a positive snowflake.");

	public static readonly Failure CannotMessageSelf =
		new("DirectMessage.CannotMessageSelf", "Sender and Recipient can't be the same id.");

	public static readonly Failure NotAFriend =
		new("DirectMessage.NotAFriend", "Sender is not a friend of the Recipient.");

	public static readonly Failure NonceTooLong =
		new("DirectMessage.NonceTooLong", "Nonce must be 64 characters or fewer.");
}
