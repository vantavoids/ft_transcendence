namespace Chat.Application.Abstractions;

public interface IGuildClient
{
	Task<ChannelMembership?> GetMembershipAsync(long channelId, long userId, CancellationToken ct);
	Task<IReadOnlyList<long>> GetVisibleChannelIdsAsync(long userId, CancellationToken ct);
}

public sealed record ChannelMembership(bool IsMember, long GuildId, long Permissions);
