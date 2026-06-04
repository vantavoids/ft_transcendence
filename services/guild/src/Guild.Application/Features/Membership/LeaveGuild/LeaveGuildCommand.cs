using Guild.Application.Abstractions.Messaging;
using Guild.Domain.Results;

namespace Guild.Application.Features.Membership.LeaveGuild;

public sealed record LeaveGuildCommand(long GuildId) : ICommand<Result>;
