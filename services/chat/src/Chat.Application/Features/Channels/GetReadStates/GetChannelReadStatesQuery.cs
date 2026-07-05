using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.Channels.Common;
using Chat.Domain.Results;

namespace Chat.Application.Features.Channels.GetReadStates;

public sealed record GetChannelReadStatesQuery : IQuery<Result<IReadOnlyList<ChannelReadStateResponse>>>;
