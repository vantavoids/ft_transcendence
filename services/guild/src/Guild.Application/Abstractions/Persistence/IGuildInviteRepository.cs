using Guild.Domain.Guild;

namespace Guild.Application.Abstractions.Persistence;

public interface IGuildInviteRepository
{
	Task<GuildInvite?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

	/// <summary>returns non-revoked invites for the guild. expired / exhausted entries are still returned so callers can decide whether to display them; <see cref="GuildInvite.IsActive"/> filters them out</summary>
	Task<IReadOnlyList<GuildInvite>> ListByGuildAsync(long guildId, CancellationToken cancellationToken = default);

	/// <summary>
	/// hard-deletes every revoked invite plus any invite whose expires_at is strictly
	/// before <paramref name="expiredBefore"/>. single bulk statement, no SaveChanges
	/// follow-up needed. returns the number of rows removed
	/// </summary>
	Task<int> DeleteRevokedAndExpiredAsync(DateTimeOffset expiredBefore, CancellationToken cancellationToken = default);

	/// <summary>
	/// deletes every invite created by <paramref name="creatorUserId"/> across all
	/// guilds in one bulk statement. used by the <c>user.deleted</c> GDPR cascade:
	/// an invite has no subject other than its creator, so erasing the creator
	/// removes the invite (invalidating the code).
	/// </summary>
	Task RemoveAllByCreatorAsync(long creatorUserId, CancellationToken cancellationToken = default);

	void Add(GuildInvite invite);
	void Update(GuildInvite invite);
}
