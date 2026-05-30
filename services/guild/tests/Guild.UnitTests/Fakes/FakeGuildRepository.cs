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
	public int SaveChangesCount { get; private set; }

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

	public Task<int> CountMembersAsync(long guildId, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(_store.TryGetValue(guildId, out var guild) ? guild.Members.Count : 0);
	}

	public Task<bool> IsMemberAsync(long guildId, long userId, CancellationToken cancellationToken = default)
	{
		if (!_store.TryGetValue(guildId, out var guild))
			return Task.FromResult(false);

		return Task.FromResult(guild.Members.Any(m => m.UserId == userId));
	}

	public Task AddAsync(GuildEntity guild, CancellationToken cancellationToken = default)
	{
		_store[guild.Id] = guild;
		AddCount++;
		return Task.CompletedTask;
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

	public Task SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		SaveChangesCount++;
		return Task.CompletedTask;
	}
}
