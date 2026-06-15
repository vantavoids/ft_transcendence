using Guild.Application.Abstractions.Messaging;
using Guild.Domain.Results;

namespace Guild.Application.Features.Membership.UnassignRole;

public sealed record UnassignRoleCommand(long GuildId, long TargetUserId, long RoleId) : ICommand<Result>;
