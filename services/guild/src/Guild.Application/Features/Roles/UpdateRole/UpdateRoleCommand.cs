using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Roles.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Roles.UpdateRole;

public sealed record UpdateRoleCommand(
	long GuildId,
	long RoleId,
	string? Name,
	string? Color,
	long? Permissions,
	int? Position,
	bool? IsHoisted,
	bool? IsMentionable) : ICommand<Result<RoleResponse>>;
