using Guild.Domain.Guild;

namespace Guild.Application.Abstractions.Persistence;

public interface IChannelPermissionOverwriteRepository
{
	Task<IReadOnlyList<ChannelPermissionOverwrite>> GetForChannelAsync(
		long channelId,
		CancellationToken cancellationToken = default);

	Task<ChannelPermissionOverwrite?> GetForChannelAndTargetAsync(
		long channelId,
		OverwriteTargetType targetType,
		long targetId,
		CancellationToken cancellationToken = default);

	Task AddAsync(ChannelPermissionOverwrite overwrite, CancellationToken cancellationToken = default);
	void Update(ChannelPermissionOverwrite overwrite);
	void Remove(ChannelPermissionOverwrite overwrite);

	Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
