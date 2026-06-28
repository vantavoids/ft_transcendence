using Guild.Domain.Guild;

namespace Guild.Application.Abstractions.Persistence;

public interface IChannelRepository
{
	Task<Channel?> GetByIdAsync(long channelId, CancellationToken cancellationToken = default);

	Task<IReadOnlyList<Channel>> GetByGuildAsync(long guildId, CancellationToken cancellationToken = default);

	/// <summary>
	/// returns the largest <c>position</c> currently stored for channels in the
	/// given <c>(guild_id, category_id)</c> bucket, or <c>null</c> when the
	/// bucket is empty. used by auto-append on create. <c>category_id</c> is
	/// <c>null</c> for uncategorised channels and compared as such
	/// </summary>
	Task<int?> GetMaxPositionAsync(
		long guildId,
		long? categoryId,
		CancellationToken cancellationToken = default);

	Task AddAsync(Channel channel, CancellationToken cancellationToken = default);
	void Update(Channel channel);
	void Remove(Channel channel);
}
