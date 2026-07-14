using Chat.Application.Features.Channels.Common;
using Chat.Application.Features.DirectMessages.Common;

namespace Chat.Application.Abstractions;

/// <summary>
/// pushes per-user real-time notifications over SignalR. impl lives in the
/// Presentation layer because <c>IHubContext</c> is an ASP.NET abstraction
/// that must not leak into Infrastructure
/// </summary>
public interface IUserBroadcaster
{
	Task BroadcastGuildJoinedAsync(long userId, long guildId, string guildName, CancellationToken ct);
	Task BroadcastGuildLeftAsync(long userId, long guildId, CancellationToken ct);

	/// <summary>
	/// tells every member of the guild:{guildId} group the guild was deleted so
	/// their client removes it live instead of showing a stale guild until
	/// refresh.
	/// </summary>
	Task BroadcastGuildDeletedAsync(long guildId, CancellationToken ct);

	/// <summary>
	/// tells the guild:{guildId} group the guild's name/icon changed so members'
	/// sidebar + header update live.
	/// </summary>
	Task BroadcastGuildUpdatedAsync(long guildId, string name, string? iconUrl, CancellationToken ct);

	/// <summary>
	/// tells the guild:{guildId} group that ownership moved from
	/// <paramref name="oldOwnerId"/> to <paramref name="newOwnerId"/>, so members'
	/// owner-only UI (crown, management controls) updates live without a refresh.
	/// </summary>
	Task BroadcastGuildOwnerTransferredAsync(long guildId, long oldOwnerId, long newOwnerId, CancellationToken ct);

	/// <summary>
	/// subscribes every open connection of <paramref name="userId"/> to the
	/// <c>guild:{guildId}</c> group so they receive that guild's real-time
	/// broadcasts. called when the user joins a guild while already connected
	/// (connect-time subscription is handled by the hub itself).
	/// </summary>
	Task AddUserToGuildGroupAsync(long userId, long guildId, CancellationToken ct);

	/// <summary>
	/// removes every open connection of <paramref name="userId"/> from the
	/// <c>guild:{guildId}</c> group when they leave / are kicked from the guild.
	/// </summary>
	Task RemoveUserFromGuildGroupAsync(long userId, long guildId, CancellationToken ct);

	Task BroadcastChannelReadStateUpdatedAsync(long userId, ChannelReadStateResponse response, CancellationToken ct);
	Task BroadcastDmReadStateUpdatedAsync(long userId, DmReadStateResponse response, CancellationToken ct);

	/// <summary>
	/// pushes a channel create/update to exactly the members who may read it
	/// (<paramref name="userIds"/>), so private channels never surface to members
	/// without read access.
	/// </summary>
	Task BroadcastChannelCreatedAsync(IReadOnlyList<long> userIds, GuildChannelDto channel, CancellationToken ct);
	Task BroadcastChannelUpdatedAsync(IReadOnlyList<long> userIds, GuildChannelDto channel, CancellationToken ct);
	Task BroadcastChannelDeletedAsync(IReadOnlyList<long> userIds, long guildId, long channelId, CancellationToken ct);

	/// <summary>
	/// notifies the guild:{guildId} group that a member joined / left, so every
	/// current member's roster updates live.
	/// </summary>
	Task BroadcastMemberJoinedAsync(long guildId, long userId, CancellationToken ct);
	Task BroadcastMemberLeftAsync(long guildId, long userId, CancellationToken ct);

	/// <summary>
	/// category lifecycle to the guild:{guildId} group. categories carry no
	/// per-member read restriction, so every member receives them.
	/// </summary>
	Task BroadcastCategoryCreatedAsync(long guildId, GuildCategoryDto category, CancellationToken ct);
	Task BroadcastCategoryUpdatedAsync(long guildId, GuildCategoryDto category, CancellationToken ct);
	Task BroadcastCategoryDeletedAsync(long guildId, long categoryId, CancellationToken ct);

	/// <summary>
	/// signals the guild:{guildId} group that its role set changed / a single
	/// member's roles or nickname changed, so clients re-fetch the affected view.
	/// </summary>
	Task BroadcastRolesChangedAsync(long guildId, CancellationToken ct);
	Task BroadcastMemberUpdatedAsync(long guildId, long userId, CancellationToken ct);

	/// <summary>
	/// tells a single user they gained read access to a channel so their client
	/// refreshes the guild's channel list and the newly-visible channel appears.
	/// mirror of the channel.access_revoked eviction.
	/// </summary>
	Task BroadcastChannelAccessGrantedAsync(long userId, long guildId, long channelId, CancellationToken ct);

	/// <summary>
	/// force-terminates every active connection of <paramref name="userId"/> at
	/// the transport level, regardless of client cooperation. used for
	/// user.logged_out/user.deleted so a stale or malicious client can't keep
	/// receiving pushes (e.g. DMs) after the session has ended.
	/// </summary>
	/// <returns>The number of connections aborted.</returns>
	Task<int> DisconnectUserAsync(long userId, CancellationToken ct);

	/// <summary>
	/// purges every connection of <paramref name="userId"/> from the SignalR
	/// groups of all channels they had joined under <paramref name="guildId"/>.
	/// used when a member is kicked/banned or leaves a guild so they stop
	/// receiving channel broadcasts even if their client never unsubscribes.
	/// </summary>
	/// <returns>The number of channels subscriptions purged.</returns>
	Task<int> EvictFromGuildChannelsAsync(long userId, long guildId, CancellationToken ct);

	/// <summary>
	/// purges every connection of <paramref name="userId"/> from the SignalR
	/// group of a single channel. used when read access to one channel is
	/// revoked without leaving the guild.
	/// </summary>
	/// <returns>The number of connections purged.</returns>
	Task<int> EvictFromChannelAsync(long userId, long channelId, CancellationToken ct);
}
