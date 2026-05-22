using Guild.Application.Abstractions.Messaging;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.Membership;

public sealed record GetChannelMembershipQuery(long ChannelId, long UserId)
	: IQuery<Result<MembershipResponse>>;

/// <summary>
/// wire shape returned by the internal <c>GET /internal/channels/{id}/membership</c>
/// endpoint. <c>GuildId</c> is included so Chat Service can publish events
/// without a second round-trip
/// </summary>
public sealed record MembershipResponse(bool IsMember, string GuildId, long Permissions);
