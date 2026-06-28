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

	/// <summary>
	/// snowflake IDs are unique across roles and members, so a single
	/// <c>(channel_id, target_id)</c> probe is sufficient to find the row
	/// regardless of its target_type. used by the DELETE handler where the URL
	/// only carries target_id
	/// </summary>
	Task<ChannelPermissionOverwrite?> GetForChannelByTargetIdAsync(
		long channelId,
		long targetId,
		CancellationToken cancellationToken = default);

	void Add(ChannelPermissionOverwrite overwrite);
	void Update(ChannelPermissionOverwrite overwrite);
	void Remove(ChannelPermissionOverwrite overwrite);
}
