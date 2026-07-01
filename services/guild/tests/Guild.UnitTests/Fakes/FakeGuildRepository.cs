using Guild.Application.Abstractions.Persistence;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Fakes;

internal sealed class FakeGuildRepository : IGuildRepository
{
	private readonly Dictionary<long, GuildEntity> _store = new();

	public IReadOnlyDictionary<long, GuildEntity> Store => _store;

	public int AddCount { get; private set; }
	public int UpdateCount { get; private set; }
	public int RemoveCount { get; private set; }

	public Task<GuildEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
	{
		_store.TryGetValue(id, out var guild);
		return Task.FromResult(guild);
	}

	public Task<GuildEntity?> GetByIdWithMembershipAsync(long id, CancellationToken cancellationToken = default)
	{
		_store.TryGetValue(id, out var guild);
		return Task.FromResult(guild);
	}

	public Task<GuildEntity?> GetByIdWithMembershipAsNoTrackingAsync(long id, CancellationToken cancellationToken = default)
	{
		// the fake holds entities by reference and does no change tracking, so
		// tracked vs no-tracking is the same object here
		_store.TryGetValue(id, out var guild);
		return Task.FromResult(guild);
	}

	public Task<IReadOnlyList<MyGuildSummary>> ListForMemberAsync(
		long userId, CancellationToken cancellationToken = default)
	{
		var summaries = _store.Values
			.Where(g => g.Members.Any(m => m.UserId == userId))
			.Select(g => new MyGuildSummary(
				g.Id,
				g.Name,
				g.IconUrl,
				g.OwnerId,
				g.Members.Count,
				g.Members.First(m => m.UserId == userId).JoinedAt))
			.ToList();

		return Task.FromResult<IReadOnlyList<MyGuildSummary>>(summaries);
	}

	public Task<int> CountMembersAsync(long guildId, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(_store.TryGetValue(guildId, out var guild) ? guild.Members.Count : 0);
	}

	public Task<IReadOnlyList<MemberPage>> PageMembersAsync(
		long guildId, long? afterUserId, int limit, CancellationToken cancellationToken = default)
	{
		if (!_store.TryGetValue(guildId, out var guild))
			return Task.FromResult<IReadOnlyList<MemberPage>>([]);

		var rolesByUser = guild.MemberRoles
			.GroupBy(mr => mr.UserId)
			.ToDictionary(g => g.Key, g => (IReadOnlyList<long>)g.Select(mr => mr.RoleId).ToList());

		var page = guild.Members
			.Where(m => afterUserId is null || m.UserId > afterUserId.Value)
			.OrderBy(m => m.UserId)
			.Take(limit)
			.Select(m => new MemberPage(
				m.UserId,
				m.Nickname,
				m.JoinedAt,
				rolesByUser.TryGetValue(m.UserId, out var ids) ? ids : []))
			.ToList();

		return Task.FromResult<IReadOnlyList<MemberPage>>(page);
	}

	public Task<int> CountOwnedByAsync(long userId, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(_store.Values.Count(g => g.OwnerId == userId));
	}

	public Task<bool> IsMemberAsync(long guildId, long userId, CancellationToken cancellationToken = default)
	{
		if (!_store.TryGetValue(guildId, out var guild))
			return Task.FromResult(false);

		return Task.FromResult(guild.Members.Any(m => m.UserId == userId));
	}

	public void Add(GuildEntity guild)
	{
		_store[guild.Id] = guild;
		AddCount++;
	}

	public void Update(GuildEntity guild)
	{
		_store[guild.Id] = guild;
		UpdateCount++;
	}

	public void Remove(GuildEntity guild)
	{
		_store.Remove(guild.Id);
		RemoveCount++;
	}

	public int PurgeMembershipCount { get; private set; }
	public long? LastPurgedMember { get; private set; }

	public Task PurgeMembershipForUserAsync(long userId, CancellationToken cancellationToken = default)
	{
		// the real repo issues bulk deletes on Members + MemberRoles; the fake holds
		// whole guild aggregates so it records the call for orchestration assertions.
		PurgeMembershipCount++;
		LastPurgedMember = userId;
		return Task.CompletedTask;
	}
}
