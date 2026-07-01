using Guild.Application.Abstractions.Persistence;
using Guild.Domain.Guild;

namespace Guild.UnitTests.Fakes;

internal sealed class FakeGuildBanRepository : IGuildBanRepository
{
	private readonly Dictionary<(long GuildId, long UserId), GuildBan> _store = new();

	public IReadOnlyDictionary<(long GuildId, long UserId), GuildBan> Store => _store;

	public int AddCount { get; private set; }
	public int RemoveCount { get; private set; }
	public int RemoveAllForUserCount { get; private set; }
	public int ScrubModeratorCount { get; private set; }
	public long? LastScrubbedModerator { get; private set; }

	public Task<IReadOnlyList<GuildBan>> ListByGuildAsync(
		long guildId, long? afterUserId, int limit, CancellationToken cancellationToken = default)
	{
		IReadOnlyList<GuildBan> rows = _store.Values
			.Where(b => b.GuildId == guildId && (afterUserId is null || b.UserId > afterUserId.Value))
			.OrderBy(b => b.UserId)
			.Take(limit)
			.ToList();
		return Task.FromResult(rows);
	}

	public Task<GuildBan?> FindAsync(long guildId, long userId, CancellationToken cancellationToken = default)
	{
		_store.TryGetValue((guildId, userId), out var ban);
		return Task.FromResult(ban);
	}

	public void Add(GuildBan ban)
	{
		if (_store.ContainsKey((ban.GuildId, ban.UserId)))
			throw new InvalidOperationException(
				$"A GuildBan with key ({ban.GuildId}, {ban.UserId}) is already tracked.");

		_store[(ban.GuildId, ban.UserId)] = ban;
		AddCount++;
	}

	public void Remove(GuildBan ban)
	{
		_store.Remove((ban.GuildId, ban.UserId));
		RemoveCount++;
	}

	public Task RemoveAllForUserAsync(long userId, CancellationToken cancellationToken = default)
	{
		var keys = _store.Keys.Where(k => k.UserId == userId).ToList();
		foreach (var key in keys)
			_store.Remove(key);
		RemoveAllForUserCount++;
		return Task.CompletedTask;
	}

	public Task ScrubModeratorAsync(long moderatorUserId, CancellationToken cancellationToken = default)
	{
		// the real repo nulls banned_by via bulk ExecuteUpdate; GuildBan.BannedBy is
		// private-set so the fake records the invocation for orchestration assertions
		// rather than mutating entities.
		ScrubModeratorCount++;
		LastScrubbedModerator = moderatorUserId;
		return Task.CompletedTask;
	}

	internal void Seed(GuildBan ban) => _store[(ban.GuildId, ban.UserId)] = ban;
}
