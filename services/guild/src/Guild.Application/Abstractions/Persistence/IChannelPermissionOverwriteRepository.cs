using Guild.Domain.Guild;

namespace Guild.Application.Abstractions.Persistence;

public interface IChannelPermissionOverwriteRepository
{
	Task<IReadOnlyList<ChannelPermissionOverwrite>> GetForChannelAsync(
		long channelId,
		CancellationToken cancellationToken = default);

	Task<IReadOnlyList<ChannelPermissionOverwrite>> GetForGuildAsync(
		long guildId,
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
