using Guild.Application.Abstractions.Persistence;
using Guild.Domain.Guild;

namespace Guild.UnitTests.Fakes;

internal sealed class FakeChannelCategoryRepository : IChannelCategoryRepository
{
	private readonly Dictionary<long, ChannelCategory> _store = new();

	public IReadOnlyDictionary<long, ChannelCategory> Store => _store;

	public int AddCount { get; private set; }
	public int UpdateCount { get; private set; }
	public int RemoveCount { get; private set; }
	public int SaveChangesCount { get; private set; }

	public Task<ChannelCategory?> GetByIdAsync(
		long guildId,
		long categoryId,
		CancellationToken cancellationToken = default)
	{
		_store.TryGetValue(categoryId, out var category);
		if (category is null || category.GuildId != guildId)
			return Task.FromResult<ChannelCategory?>(null);
		return Task.FromResult<ChannelCategory?>(category);
	}

	public Task<int?> GetMaxPositionAsync(long guildId, CancellationToken cancellationToken = default)
	{
		var positions = _store.Values
			.Where(c => c.GuildId == guildId)
			.Select(c => c.Position)
			.ToList();
		int? result = positions.Count == 0 ? null : positions.Max();
		return Task.FromResult(result);
	}

	public Task AddAsync(ChannelCategory category, CancellationToken cancellationToken = default)
	{
		_store[category.Id] = category;
		AddCount++;
		return Task.CompletedTask;
	}

	public void Update(ChannelCategory category)
	{
		_store[category.Id] = category;
		UpdateCount++;
	}

	public void Remove(ChannelCategory category)
	{
		_store.Remove(category.Id);
		RemoveCount++;
	}

	public Task SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		SaveChangesCount++;
		return Task.CompletedTask;
	}

	internal void Seed(ChannelCategory category) => _store[category.Id] = category;
}
