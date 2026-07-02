namespace Chat.Application.Features.Messages.SendMessage;

/// <summary>the fields a "send" command has regardless of where it's headed</summary>
public interface ISendMessageCommand
{
	string? Content { get; }
	long? ReplyToId { get; }
	IReadOnlyList<long> AttachmentIds { get; }
	string? Nonce { get; }
}
