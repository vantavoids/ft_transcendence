namespace Chat.Application.Abstractions;

public interface IGuildClient
{
	Task<ChannelMembership?> GetMembershipAsync(Guid channelId, Guid userId, CancellationToken ct);
}

public sealed record ChannelMembership(bool IsMember, long Permissions);
