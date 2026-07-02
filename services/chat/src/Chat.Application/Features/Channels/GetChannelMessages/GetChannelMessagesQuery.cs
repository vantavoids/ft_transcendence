using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.Channels.Common;
using Chat.Domain.Results;

namespace Chat.Application.Features.Channels.GetChannelMessages;

public sealed record GetChannelMessagesQuery(long ChannelId, DateTimeOffset? BeforeTime, int Limit)
	: IQuery<Result<IReadOnlyList<ChannelMessageResponse>>>;
