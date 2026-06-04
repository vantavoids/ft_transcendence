using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Invites.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Invites.CreateInvite;

public sealed record CreateInviteCommand(
	long GuildId,
	int? MaxUses,
	int? ExpiresInHours)
	: ICommand<Result<InviteDto>>;
