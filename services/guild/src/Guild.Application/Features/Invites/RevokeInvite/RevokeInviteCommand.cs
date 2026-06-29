using Guild.Application.Abstractions.Messaging;
using Guild.Domain.Results;

namespace Guild.Application.Features.Invites.RevokeInvite;

public sealed record RevokeInviteCommand(long GuildId, string Code) : ICommand<Result>;
