using Chat.Application.Abstractions;

namespace Chat.UnitTests.Fakes;

public sealed class FakeUserBroadcaster : IUserBroadcaster
{
	private readonly List<(long UserId, long GuildId, string GuildName)> _joinCalls = [];
	private readonly List<(long UserId, long GuildId)> _leftCalls = [];

	public IReadOnlyList<(long UserId, long GuildId, string GuildName)> JoinCalls => _joinCalls;
	public IReadOnlyList<(long UserId, long GuildId)> LeftCalls => _leftCalls;

	public Task BroadcastGuildJoinedAsync(long userId, long guildId, string guildName, CancellationToken ct)
	{
		_joinCalls.Add((userId, guildId, guildName));
		return Task.CompletedTask;
	}

	public Task BroadcastGuildLeftAsync(long userId, long guildId, CancellationToken ct)
	{
		_leftCalls.Add((userId, guildId));
		return Task.CompletedTask;
	}
}
