namespace Chat.Domain.Reactions;

/// <summary>wire-and-storage shape of one emoji's reaction summary on a message</summary>
public sealed record ReactionSummary(string Emoji, long Count, bool MeReacted);
