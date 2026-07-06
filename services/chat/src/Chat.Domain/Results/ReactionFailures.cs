using Chat.Domain.Reactions;

namespace Chat.Domain.Results;

public static class ReactionFailures
{
	public static readonly Failure EmojiRequired =
		new("Reaction.EmojiRequired", "Emoji is required.");

	public static readonly Failure EmojiTooLong =
		new("Reaction.EmojiTooLong", $"Emoji must be {ReactionEmoji.MaxEmojiLen} characters or fewer.");

	public static readonly Failure IsDirectMessage =
		new("Reaction.IsDirectMessage", "Reactions are not supported on direct messages.");
}
