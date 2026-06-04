using Guild.Application.Abstractions.Messaging;
using Guild.Domain.Results;

namespace Guild.Application.Features.Invites.DeleteInvite;

public sealed record DeleteInviteCommand(long GuildId, string Code) : ICommand<Result>;
