using Chat.Domain.Messages;

namespace Chat.Domain.Results;

public static class MessageFailures
{
	public static readonly Failure ContentRequired =
		new("Message.ContentRequired", "Message content is required.");

	public static readonly Failure ContentTooLong =
		new("Message.ContentTooLong",
			$"Message content must be {Message.MaxContentLen} characters or fewer.");

	public static readonly Failure InvalidId =
		new("Message.InvalidId", "Message id must be a positive snowflake.");

	public static readonly Failure InvalidChannelId =
		new("Message.InvalidChannelId", "Channel id must be a positive snowflake.");

	public static readonly Failure InvalidAuthorId =
		new("Message.InvalidAuthorId", "Author id must be a positive snowflake.");

	public static readonly Failure ChannelNotFound =
		new("Message.ChannelNotFound", "Channel not found.");

	public static readonly Failure NotAMember =
		new("Message.NotAMember", "Caller is not a member of the guild that owns this channel.");

	public static readonly Failure MissingSendPermission =
		new("Message.MissingSendPermission", "Caller lacks SEND_MESSAGES permission on this channel.");

	public static readonly Failure NonceTooLong =
		new("Message.NonceTooLong", "Nonce must be 64 characters or fewer.");

	public static readonly Failure InvalidReplyTarget =
		new("Message.InvalidReplyTarget", "Reply target was not found in this channel.");

	public static readonly Failure NotFound =
		new("Message.NotFound", "Message not found.");

	public static readonly Failure NotAuthor =
		new("Message.NotAuthor", "Caller is not the author of this message.");

	public static readonly Failure AlreadyDeleted =
		new("Message.AlreadyDeleted", "Message has already been deleted.");

	public static readonly Failure MissingManagePermission =
		new("Message.MissingManagePermission", "Caller lacks MANAGE_MESSAGES permission on this channel.");

	public static readonly Failure MissingReadPermission =
		new("Message.MissingReadPermission", "Caller lacks READ_MESSAGES permission on this channel.");

	public static readonly Failure UserNotFound =
		new("Message.UserNotFound", "User not found.");

	public static readonly Failure RecipientNotFriend =
		new("Message.RecipientNotFriend", "You must be friends to send direct message.");

	public static readonly Failure RecipientBlocked =
		new("Message.RecipientBlocked", "You can't send messages to blocked user.");
}
