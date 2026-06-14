using Guild.Application.Abstractions.Messaging;
using Guild.Domain.Results;

namespace Guild.Application.Features.Roles.DeleteRole;

public sealed record DeleteRoleCommand(long GuildId, long RoleId) : ICommand<Result>;
