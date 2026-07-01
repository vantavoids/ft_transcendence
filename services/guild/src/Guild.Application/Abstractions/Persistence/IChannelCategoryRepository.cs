using Guild.Domain.Guild;

namespace Guild.Application.Abstractions.Persistence;

public interface IChannelCategoryRepository
{
	Task<ChannelCategory?> GetByIdAsync(
		long guildId,
		long categoryId,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// returns this guild's categories ordered by <c>position</c> ascending, as a
	/// read-only (no change tracking) projection for the list endpoint
	/// </summary>
	Task<IReadOnlyList<ChannelCategory>> GetByGuildAsync(
		long guildId,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// returns the largest <c>position</c> currently stored for this guild's
	/// categories, or <c>null</c> if the guild has no categories yet. used by
	/// auto-append logic when <c>position</c> is omitted on create
	/// </summary>
	Task<int?> GetMaxPositionAsync(long guildId, CancellationToken cancellationToken = default);

	void Add(ChannelCategory category);
	void Update(ChannelCategory category);
	void Remove(ChannelCategory category);
}
