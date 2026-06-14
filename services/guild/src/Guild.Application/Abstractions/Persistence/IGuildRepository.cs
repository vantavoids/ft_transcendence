using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.Application.Abstractions.Persistence;

public interface IGuildRepository
{
	/// <summary>loads the bare Guild row without related collections</summary>
	Task<GuildEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

	/// <summary>loads the Guild plus Members, Roles and MemberRoles so PermissionResolver can run</summary>
	Task<GuildEntity?> GetByIdWithMembershipAsync(long id, CancellationToken cancellationToken = default);

	Task<int> CountMembersAsync(long guildId, CancellationToken cancellationToken = default);
	Task<int> CountOwnedByAsync(long userId, CancellationToken cancellationToken = default);
	Task<bool> IsMemberAsync(long guildId, long userId, CancellationToken cancellationToken = default);

	Task AddAsync(GuildEntity guild, CancellationToken cancellationToken = default);
	void Update(GuildEntity guild);
	void Remove(GuildEntity guild);

	Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
