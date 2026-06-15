using Guild.Application.Abstractions.Messaging;
using Guild.Domain.Results;

namespace Guild.Application.Features.Membership.AssignRole;

public sealed record AssignRoleCommand(long GuildId, long TargetUserId, long RoleId) : ICommand<Result>;
