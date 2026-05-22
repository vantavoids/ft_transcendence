using Guild.Application.Abstractions.Persistence;
using Guild.Domain.Guild;

namespace Guild.UnitTests.Fakes;

internal sealed class FakeChannelPermissionOverwriteRepository : IChannelPermissionOverwriteRepository
{
	private readonly Dictionary<long, ChannelPermissionOverwrite> _store = new();

	public IReadOnlyDictionary<long, ChannelPermissionOverwrite> Store => _store;

	public Task<IReadOnlyList<ChannelPermissionOverwrite>> GetForChannelAsync(
		long channelId,
		CancellationToken cancellationToken = default)
	{
		IReadOnlyList<ChannelPermissionOverwrite> list = _store.Values
			.Where(o => o.ChannelId == channelId)
			.ToList();
		return Task.FromResult(list);
	}

	public Task<ChannelPermissionOverwrite?> GetForChannelAndTargetAsync(
		long channelId,
		OverwriteTargetType targetType,
		long targetId,
		CancellationToken cancellationToken = default)
	{
		var match = _store.Values.FirstOrDefault(o =>
			o.ChannelId == channelId
			&& o.TargetType == targetType
			&& o.TargetId == targetId);
		return Task.FromResult(match);
	}

	public Task<ChannelPermissionOverwrite?> GetForChannelByTargetIdAsync(
		long channelId,
		long targetId,
		CancellationToken cancellationToken = default)
	{
		var match = _store.Values.FirstOrDefault(o =>
			o.ChannelId == channelId && o.TargetId == targetId);
		return Task.FromResult(match);
	}

	public Task AddAsync(ChannelPermissionOverwrite overwrite, CancellationToken cancellationToken = default)
	{
		_store[overwrite.Id] = overwrite;
		return Task.CompletedTask;
	}

	public void Update(ChannelPermissionOverwrite overwrite)
	{
		_store[overwrite.Id] = overwrite;
	}

	public void Remove(ChannelPermissionOverwrite overwrite)
	{
		_store.Remove(overwrite.Id);
	}

	public Task SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	internal void Seed(ChannelPermissionOverwrite overwrite) => _store[overwrite.Id] = overwrite;
}
