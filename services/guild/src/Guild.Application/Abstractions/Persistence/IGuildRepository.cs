using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.Application.Abstractions.Persistence;

public interface IGuildRepository
{
	/// <summary>loads the bare Guild row without related collections</summary>
	Task<GuildEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

	/// <summary>loads the Guild plus Members, Roles and MemberRoles so PermissionResolver can run</summary>
	Task<GuildEntity?> GetByIdWithMembershipAsync(long id, CancellationToken cancellationToken = default);

	/// <summary>
	/// same shape as <see cref="GetByIdWithMembershipAsync"/> but read-only: no
	/// change tracking and a split query. for handlers that only read the
	/// aggregate to resolve permissions and never mutate it.
	/// </summary>
	Task<GuildEntity?> GetByIdWithMembershipAsNoTrackingAsync(long id, CancellationToken cancellationToken = default);

	Task<int> CountMembersAsync(long guildId, CancellationToken cancellationToken = default);
	Task<int> CountOwnedByAsync(long userId, CancellationToken cancellationToken = default);
	Task<bool> IsMemberAsync(long guildId, long userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// keyset page of members (ordered by user id, cursor = <paramref name="afterUserId"/>)
	/// projected straight to <see cref="MemberPage"/> in the database, so listing
	/// members never hydrates the whole guild aggregate into memory.
	/// </summary>
	Task<IReadOnlyList<MemberPage>> PageMembersAsync(
		long guildId, long? afterUserId, int limit, CancellationToken cancellationToken = default);

	Task AddAsync(GuildEntity guild, CancellationToken cancellationToken = default);
	void Update(GuildEntity guild);
	void Remove(GuildEntity guild);

	Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// flat projection of one guild member for list endpoints: the member's own
/// columns plus the ids of their explicitly assigned roles (the implicit
/// @everyone is not included, matching the wire contract).
/// </summary>
public sealed record MemberPage(
	long UserId,
	string? Nickname,
	DateTimeOffset JoinedAt,
	IReadOnlyList<long> RoleIds);
