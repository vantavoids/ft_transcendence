using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Channels.ListChannels;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.VisibleChannels;

public sealed record GetVisibleChannelsQuery(long UserId) : IQuery<Result<ChannelListResponse>>;
