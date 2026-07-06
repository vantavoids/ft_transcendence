using Chat.Domain.Results;

namespace Chat.Domain.Reactions;

/// <summary>
/// Reactions have no rich aggregate of their own - a reaction is just a
/// (message, emoji, user) key plus a counter, both handled at the persistence
/// layer. This holds the one piece of domain logic that isn't storage: emoji
/// format validation.
/// </summary>
public static class ReactionEmoji
{
	public const int MaxEmojiLen = 32;

	public static Result<string> Validate(string? emoji)
	{
		if (string.IsNullOrWhiteSpace(emoji))
			return ReactionFailures.EmojiRequired;

		if (emoji.Length > MaxEmojiLen)
			return ReactionFailures.EmojiTooLong;

		return emoji;
	}
}
