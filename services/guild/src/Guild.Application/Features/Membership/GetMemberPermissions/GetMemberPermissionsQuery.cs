using Guild.Application.Abstractions.Messaging;
using Guild.Domain.Results;

namespace Guild.Application.Features.Membership.GetMemberPermissions;

public sealed record GetMemberPermissionsQuery(long GuildId, long TargetUserId)
	: IQuery<Result<MemberPermissionsResponse>>;
