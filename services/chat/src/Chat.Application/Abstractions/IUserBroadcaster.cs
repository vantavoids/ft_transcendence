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

	Task BroadcastChannelReadStateUpdatedAsync(long userId, ChannelReadStateResponse response, CancellationToken ct);
	Task BroadcastDmReadStateUpdatedAsync(long userId, DmReadStateResponse response, CancellationToken ct);

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
