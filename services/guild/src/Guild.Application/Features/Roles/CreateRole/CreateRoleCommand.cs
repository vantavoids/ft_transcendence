using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Roles.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Roles.CreateRole;

public sealed record CreateRoleCommand(
	long GuildId,
	string? Name,
	string? Color,
	long Permissions,
	bool IsHoisted,
	bool IsMentionable) : ICommand<Result<RoleResponse>>;
