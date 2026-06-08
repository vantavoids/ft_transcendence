using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.Messages.Common;
using Chat.Domain.Results;

namespace Chat.Application.Features.Messages.GetChannelMessages;

public sealed record GetChannelMessagesQuery(long ChannelId, DateTimeOffset? BeforeTime, int Limit)
	: IQuery<Result<IReadOnlyList<MessageResponse>>>;
