using Guild.Application.Abstractions.Persistence;
using Guild.Domain.Guild;

namespace Guild.UnitTests.Fakes;

internal sealed class FakeGuildInviteRepository : IGuildInviteRepository
{
	private readonly Dictionary<string, GuildInvite> _store = new();

	public IReadOnlyDictionary<string, GuildInvite> Store => _store;

	public int AddCount { get; private set; }
	public int UpdateCount { get; private set; }

	public Task<GuildInvite?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
	{
		_store.TryGetValue(code, out var invite);
		return Task.FromResult(invite);
	}

	public Task<IReadOnlyList<GuildInvite>> ListByGuildAsync(long guildId, CancellationToken cancellationToken = default)
	{
		IReadOnlyList<GuildInvite> rows = _store.Values
			.Where(i => i.GuildId == guildId && !i.IsRevoked)
			.OrderByDescending(i => i.CreatedAt)
			.ToList();
		return Task.FromResult(rows);
	}

	public void Add(GuildInvite invite)
	{
		_store[invite.Code] = invite;
		AddCount++;
	}

	public void Update(GuildInvite invite)
	{
		_store[invite.Code] = invite;
		UpdateCount++;
	}

	public Task<int> DeleteRevokedAndExpiredAsync(DateTimeOffset expiredBefore, CancellationToken cancellationToken = default)
	{
		var doomed = _store.Values
			.Where(i => i.IsRevoked || (i.ExpiresAt is { } e && e < expiredBefore))
			.Select(i => i.Code).ToList();
		foreach (var code in doomed) _store.Remove(code);
		return Task.FromResult(doomed.Count);
	}

	public int RemoveAllByCreatorCount { get; private set; }

	public Task RemoveAllByCreatorAsync(long creatorUserId, CancellationToken cancellationToken = default)
	{
		var doomed = _store.Values.Where(i => i.CreatedBy == creatorUserId).Select(i => i.Code).ToList();
		foreach (var code in doomed) _store.Remove(code);
		RemoveAllByCreatorCount++;
		return Task.CompletedTask;
	}

	internal void Seed(GuildInvite invite) => _store[invite.Code] = invite;
}
