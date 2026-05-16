using Guild.Application.Abstractions.Messaging;
using Guild.Domain.Results;

namespace Guild.Application.Features.Guilds.DeleteGuild;

public sealed record DeleteGuildCommand(long GuildId) : ICommand<Result>;
