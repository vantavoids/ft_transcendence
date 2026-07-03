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
	/// purge every connection of <paramref name="userId"/> from the SignalR
	/// groups of all channels they had joined.
	/// </summary>
	/// <returns>The number of channels subscriptions purged.</returns>
	Task<int> EvictUserFromActiveChannels(long userId, CancellationToken ct);

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
