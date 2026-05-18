using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Channels.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.ListChannels;

public sealed record ListChannelsQuery(long GuildId) : IQuery<Result<ChannelListResponse>>;

/// <summary>
/// wrapped in a record so the generic <c>IQueryHandler&lt;,&gt;</c> constraint
/// (<c>TResponse : class</c>) is satisfied; deserialised by Carter as a JSON
/// array via <see cref="ChannelListResponse.Items"/>
/// </summary>
public sealed record ChannelListResponse(IReadOnlyList<ChannelResponse> Items);
