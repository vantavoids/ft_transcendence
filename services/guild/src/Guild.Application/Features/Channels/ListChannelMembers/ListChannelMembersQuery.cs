using Guild.Application.Abstractions.Messaging;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.ListChannelMembers;

public sealed record ListChannelMembersQuery(long GuildId, long ChannelId)
	: IQuery<Result<ChannelMembersResponse>>;

/// <summary>
/// wire shape for <c>GET /guilds/{id}/channels/{channelId}/members</c>: the
/// snowflake ids of the members who can READ the channel, so the client can
/// scope the guild member list to who can actually see the current channel.
/// </summary>
public sealed record ChannelMembersResponse(IReadOnlyList<string> UserIds);
