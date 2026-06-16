using Guild.Application.Abstractions.Messaging;
using Guild.Domain.Results;

namespace Guild.Application.Features.Bans.BanMember;

public sealed record BanMemberCommand(long GuildId, long TargetUserId, string? Reason) : ICommand<Result>;
