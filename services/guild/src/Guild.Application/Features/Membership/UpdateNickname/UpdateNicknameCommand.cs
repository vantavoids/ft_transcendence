using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Membership.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Membership.UpdateNickname;

public sealed record UpdateNicknameCommand(
	long GuildId,
	long TargetUserId,
	string? Nickname) : ICommand<Result<MemberResponse>>;
