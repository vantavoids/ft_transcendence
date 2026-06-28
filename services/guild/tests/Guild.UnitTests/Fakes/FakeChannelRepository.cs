using Guild.Application.Abstractions.Persistence;
using Guild.Domain.Guild;

namespace Guild.UnitTests.Fakes;

internal sealed class FakeChannelRepository : IChannelRepository
{
	private readonly Dictionary<long, Channel> _store = new();

	public IReadOnlyDictionary<long, Channel> Store => _store;

	public int AddCount { get; private set; }
	public int UpdateCount { get; private set; }
	public int RemoveCount { get; private set; }

	public Task<Channel?> GetByIdAsync(long channelId, CancellationToken cancellationToken = default)
	{
		_store.TryGetValue(channelId, out var channel);
		return Task.FromResult(channel);
	}

	public Task<IReadOnlyList<Channel>> GetByGuildAsync(long guildId, CancellationToken cancellationToken = default)
	{
		IReadOnlyList<Channel> list = _store.Values
			.Where(c => c.GuildId == guildId)
			.OrderBy(c => c.Position)
			.ToList();
		return Task.FromResult(list);
	}

	public Task<int?> GetMaxPositionAsync(
		long guildId,
		long? categoryId,
		CancellationToken cancellationToken = default)
	{
		var positions = _store.Values
			.Where(c => c.GuildId == guildId && c.CategoryId == categoryId)
			.Select(c => c.Position)
			.ToList();
		int? result = positions.Count == 0 ? null : positions.Max();
		return Task.FromResult(result);
	}

	public Task AddAsync(Channel channel, CancellationToken cancellationToken = default)
	{
		_store[channel.Id] = channel;
		AddCount++;
		return Task.CompletedTask;
	}

	public void Update(Channel channel)
	{
		_store[channel.Id] = channel;
		UpdateCount++;
	}

	public void Remove(Channel channel)
	{
		_store.Remove(channel.Id);
		RemoveCount++;
	}

	internal void Seed(Channel channel) => _store[channel.Id] = channel;
}
