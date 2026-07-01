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

	void Add(GuildBan ban);

	void Remove(GuildBan ban);

	/// <summary>
	/// deletes every ban whose <c>user_id</c> (the banned subject) is
	/// <paramref name="userId"/>, across all guilds, in one SQL statement
	/// (bypasses the change tracker). used by the <c>user.deleted</c> GDPR cascade.
	/// note: <c>ExecuteDeleteAsync</c> is unsupported by the InMemory provider, so
	/// functional tests cannot exercise this path without a real database.
	/// </summary>
	Task RemoveAllForUserAsync(long userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// GDPR erasure: nulls <c>banned_by</c> on every ban issued by
	/// <paramref name="moderatorUserId"/> (a ban against someone still present).
	/// the ban stays in force; only the reference to the now-deleted moderator is
	/// scrubbed. one bulk <c>ExecuteUpdateAsync</c> statement.
	/// </summary>
	Task ScrubModeratorAsync(long moderatorUserId, CancellationToken cancellationToken = default);
}
