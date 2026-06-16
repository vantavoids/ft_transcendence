using Guild.Application.Abstractions.Messaging;
using Guild.Domain.Results;

namespace Guild.Application.Features.Bans.UnbanMember;

public sealed record UnbanMemberCommand(long GuildId, long TargetUserId) : ICommand<Result>;
