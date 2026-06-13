using Guild.Application.Abstractions.Messaging;
using Guild.Domain.Results;

namespace Guild.Application.Features.Membership.KickMember;

public sealed record KickMemberCommand(long GuildId, long TargetUserId) : ICommand<Result>;
