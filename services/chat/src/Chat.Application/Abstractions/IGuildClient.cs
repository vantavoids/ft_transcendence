namespace Chat.Application.Abstractions;

public interface IGuildClient
{
	Task<ChannelMembership?> GetMembershipAsync(long channelId, long userId, CancellationToken ct);
	Task<IReadOnlyList<long>> GetVisibleChannelIdsAsync(long userId, CancellationToken ct);

	/// <summary>
	/// every guild the user is a member of. used on hub connect to subscribe the
	/// connection to each <c>guild:{id}</c> group so guild-scoped broadcasts reach
	/// all members.
	/// </summary>
	Task<IReadOnlyList<long>> GetUserGuildIdsAsync(long userId, CancellationToken ct);
}

public sealed record ChannelMembership(bool IsMember, long GuildId, long Permissions);
