using Chat.Application.Abstractions;

namespace Chat.UnitTests.Fakes;

public sealed class FakeGuildClient : IGuildClient
{
	private readonly Dictionary<(long ChannelId, long UserId), ChannelMembership?> _map = new();

	// fallback returned when no per-key entry is set
	public ChannelMembership? Result { get; set; }

	public void Setup(long channelId, long userId, ChannelMembership? membership)
		=> _map[(channelId, userId)] = membership;

	public Task<ChannelMembership?> GetMembershipAsync(long channelId, long userId, CancellationToken ct)
		=> Task.FromResult(_map.TryGetValue((channelId, userId), out var m) ? m : Result);

	public List<long> VisibleChannelIds { get; set; } = [];

	public Task<IReadOnlyList<long>> GetVisibleChannelIdsAsync(long userId, CancellationToken ct)
		=> Task.FromResult<IReadOnlyList<long>>(VisibleChannelIds);

	public List<long> UserGuildIds { get; set; } = [];

	public Task<IReadOnlyList<long>> GetUserGuildIdsAsync(long userId, CancellationToken ct)
		=> Task.FromResult<IReadOnlyList<long>>(UserGuildIds);
}
