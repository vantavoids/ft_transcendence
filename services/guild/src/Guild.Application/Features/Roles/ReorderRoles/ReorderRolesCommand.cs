using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Roles.ListRoles;
using Guild.Domain.Results;

namespace Guild.Application.Features.Roles.ReorderRoles;

/// <summary>
/// bulk role reorder. <see cref="Moves"/> must list every non-@everyone role in
/// the guild exactly once; the domain validates that the positions form a
/// contiguous 1..N permutation. reuses <see cref="RoleListResponse"/> so the
/// endpoint can return the full, freshly-ordered role list (issue #213)
/// </summary>
public sealed record ReorderRolesCommand(
	long GuildId,
	IReadOnlyList<RolePositionEntry> Moves) : ICommand<Result<RoleListResponse>>;

public sealed record RolePositionEntry(long RoleId, int Position);
