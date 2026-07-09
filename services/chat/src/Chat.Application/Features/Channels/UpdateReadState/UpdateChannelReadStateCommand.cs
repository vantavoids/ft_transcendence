using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.Channels.Common;
using Chat.Domain.Results;

namespace Chat.Application.Features.Channels.UpdateReadState;

public sealed record UpdateChannelReadStateCommand(long ChannelId, long MessageId)
	: ICommand<Result<ChannelReadStateResponse>>;
