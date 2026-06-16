using Guild.Domain.Guild;

namespace Guild.Application.Abstractions.Persistence;

public interface IGuildBanRepository
{
	/// <summary>
	/// returns bans for <paramref name="guildId"/> ordered by <c>user_id</c>
	/// ascending. <paramref name="afterUserId"/> is the cursor (only rows with
	/// <c>user_id &gt; afterUserId</c> are returned); <paramref name="limit"/>
	/// caps the page size.
	/// </summary>
	Task<IReadOnlyList<GuildBan>> ListByGuildAsync(
		long guildId,
		long? afterUserId,
		int limit,
		CancellationToken cancellationToken = default);

	Task<GuildBan?> FindAsync(long guildId, long userId, CancellationToken cancellationToken = default);

	Task AddAsync(GuildBan ban, CancellationToken cancellationToken = default);

	void Remove(GuildBan ban);

	/// <summary>
	/// deletes every ban row referencing <paramref name="userId"/> across all
	/// guilds in one SQL statement (bypasses the change tracker). pre-staged
	/// for the upcoming <c>user.deleted</c> consumer in issue #147; no current
	/// caller in this commit. note: <c>ExecuteDeleteAsync</c> is unsupported by
	/// the InMemory provider, so functional tests cannot exercise this path
	/// without a real database.
	/// </summary>
	Task RemoveAllForUserAsync(long userId, CancellationToken cancellationToken = default);
}
