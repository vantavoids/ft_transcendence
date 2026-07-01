using Guild.Application.Abstractions.Persistence;
using Guild.Domain.Guild;

namespace Guild.UnitTests.Fakes;

internal sealed class FakeChannelPermissionOverwriteRepository : IChannelPermissionOverwriteRepository
{
	private readonly Dictionary<long, ChannelPermissionOverwrite> _store = new();

	public IReadOnlyDictionary<long, ChannelPermissionOverwrite> Store => _store;

	public int AddCount { get; private set; }
	public int UpdateCount { get; private set; }
	public int RemoveCount { get; private set; }

	public Task<IReadOnlyList<ChannelPermissionOverwrite>> GetForChannelAsync(
		long channelId,
		CancellationToken cancellationToken = default)
	{
		IReadOnlyList<ChannelPermissionOverwrite> list = _store.Values
			.Where(o => o.ChannelId == channelId)
			.ToList();
		return Task.FromResult(list);
	}

	public Task<IReadOnlyList<ChannelPermissionOverwrite>> GetForGuildAsync(
		long guildId,
		CancellationToken cancellationToken = default)
	{
		IReadOnlyList<ChannelPermissionOverwrite> list = _store.Values
			.Where(o => o.GuildId == guildId)
			.ToList();
		return Task.FromResult(list);
	}

	public void Add(ChannelPermissionOverwrite overwrite)
	{
		_store[overwrite.Id] = overwrite;
		AddCount++;
	}

	public void Update(ChannelPermissionOverwrite overwrite)
	{
		_store[overwrite.Id] = overwrite;
		UpdateCount++;
	}

	public int RemoveAllForMemberCount { get; private set; }

	public Task RemoveAllForMemberAsync(long userId, CancellationToken cancellationToken = default)
	{
		var doomed = _store.Values
			.Where(o => o.TargetType == OverwriteTargetType.Member && o.TargetId == userId)
			.Select(o => o.Id).ToList();
		foreach (var id in doomed) _store.Remove(id);
		RemoveAllForMemberCount++;
		return Task.CompletedTask;
	}

	public void Remove(ChannelPermissionOverwrite overwrite)
	{
		_store.Remove(overwrite.Id);
		RemoveCount++;
	}

	internal void Seed(ChannelPermissionOverwrite overwrite) => _store[overwrite.Id] = overwrite;
}
