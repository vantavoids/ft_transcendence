using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.Channels.Common;
using Chat.Domain.Results;

namespace Chat.Application.Features.Channels.SendMessage;

public sealed record SendChannelMessageCommand(
	long ChannelId,
	string? Content,
	long? ReplyToId,
	IReadOnlyList<long> AttachmentIds,
	string? Nonce)
	: ICommand<Result<ChannelMessageResponse>>;
