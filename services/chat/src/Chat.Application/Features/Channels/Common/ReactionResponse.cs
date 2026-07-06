using Chat.Domain.Reactions;

namespace Chat.Application.Features.Channels.Common;

/// <summary>wire shape for one entry in a message's <c>reactions[]</c>, per the contract</summary>
public sealed record ReactionResponse(string Emoji, long Count, bool MeReacted)
{
	public static ReactionResponse From(ReactionSummary s) => new(s.Emoji, s.Count, s.MeReacted);
}
