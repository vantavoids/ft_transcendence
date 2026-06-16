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
}
