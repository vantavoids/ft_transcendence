using Chat.Domain.Attachments;
using Chat.Domain.Messages;
using Chat.Domain.Reactions;

namespace Chat.Application.Features.Messages.SendMessage;

/// <summary>
/// lets <see cref="SendMessageHandlerBase{TCommand,TResponse,TContext}"/> build
/// the final wire response without knowing which concrete type it's building —
/// resolved at compile time per handler, not via a runtime cast
/// </summary>
public interface IMessageWireResponse<out TSelf> where TSelf : IMessageWireResponse<TSelf>
{
	static abstract TSelf From(
		Message m,
		string? nonce,
		IReadOnlyList<AttachmentMetadata>? attachments,
		IReadOnlyList<ReactionSummary>? reactions);
}
