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

	/// <summary>
	/// deletes every member-target overwrite (<c>target_type = 'user'</c>) whose
	/// <c>target_id</c> is <paramref name="userId"/>, across all channels, in one
	/// bulk statement. used by the <c>user.deleted</c> GDPR cascade to drop a
	/// deleted user's per-channel permission rows.
	/// </summary>
	Task RemoveAllForMemberAsync(long userId, CancellationToken cancellationToken = default);

	void Add(ChannelPermissionOverwrite overwrite);
	void Update(ChannelPermissionOverwrite overwrite);
	void Remove(ChannelPermissionOverwrite overwrite);
}
