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

	/// <summary>
	/// summary of every guild <paramref name="userId"/> is a member of, projected
	/// straight to <see cref="MyGuildSummary"/> in the database (id, name, icon,
	/// owner, member count, and the caller's own <c>joined_at</c>) so the sidebar
	/// listing never hydrates whole guild aggregates. order is unspecified.
	/// </summary>
	Task<IReadOnlyList<MyGuildSummary>> ListForMemberAsync(
		long userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// bare guild ids the user is a member of - no joined columns, no member
	/// count. used by callers that only need to iterate guilds to resolve
	/// per-guild data themselves (e.g. the visible-channels lookup), where
	/// <see cref="ListForMemberAsync"/>'s correlated COUNT would be wasted work.
	/// </summary>
	Task<IReadOnlyList<long>> ListGuildIdsForMemberAsync(
		long userId, CancellationToken cancellationToken = default);

	Task<int> CountMembersAsync(long guildId, CancellationToken cancellationToken = default);
	Task<int> CountOwnedByAsync(long userId, CancellationToken cancellationToken = default);
	Task<bool> IsMemberAsync(long guildId, long userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// deletes a user's <c>member_roles</c> and <c>guild_members</c> rows across all
	/// guilds in bulk statements. used by the <c>user.deleted</c> GDPR cascade.
	/// role rows go first so no membership is ever observed without its role links.
	/// Auth's 409 gate guarantees the user owns no guilds, so no ownership handling
	/// is needed here.
	/// </summary>
	Task PurgeMembershipForUserAsync(long userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// keyset page of members (ordered by user id, cursor = <paramref name="afterUserId"/>)
	/// projected straight to <see cref="MemberPage"/> in the database, so listing
	/// members never hydrates the whole guild aggregate into memory.
	/// </summary>
	Task<IReadOnlyList<MemberPage>> PageMembersAsync(
		long guildId, long? afterUserId, int limit, CancellationToken cancellationToken = default);

	/// <summary>
	/// the user's Guild-owned data for a GDPR export: guilds they own and the
	/// guilds they belong to (with their nickname and role names). human-readable
	/// names, not ids; excludes moderation and internal-config rows (bans,
	/// permission overwrites) which are not the subject's own intelligible data.
	/// </summary>
	Task<UserGuildDataExport> GetUserDataExportAsync(long userId, CancellationToken cancellationToken = default);

	void Add(GuildEntity guild);
	void Update(GuildEntity guild);
	void Remove(GuildEntity guild);
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

/// <summary>
/// flat projection of one guild for the caller's sidebar listing: the guild's
/// summary columns, its current member count, and the caller's own join time.
/// </summary>
public sealed record MyGuildSummary(
	long Id,
	string Name,
	string? IconUrl,
	long OwnerId,
	int MemberCount,
	DateTimeOffset JoinedAt);

/// <summary>a user's Guild-owned data for a GDPR export.</summary>
public sealed record UserGuildDataExport(
	IReadOnlyList<ExportedGuild> OwnedGuilds,
	IReadOnlyList<ExportedMembership> Memberships);

public sealed record ExportedGuild(string Name, DateTimeOffset CreatedAt);

public sealed record ExportedMembership(
	string GuildName,
	string? Nickname,
	DateTimeOffset JoinedAt,
	IReadOnlyList<string> Roles);
