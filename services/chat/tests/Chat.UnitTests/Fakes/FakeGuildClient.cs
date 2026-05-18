using Chat.Application.Abstractions;

namespace Chat.UnitTests.Fakes;

public sealed class FakeGuildClient : IGuildClient
{
	public ChannelMembership? Result { get; set; }

	public Task<ChannelMembership?> GetMembershipAsync(long channelId, long userId, CancellationToken ct)
		=> Task.FromResult(Result);
}
