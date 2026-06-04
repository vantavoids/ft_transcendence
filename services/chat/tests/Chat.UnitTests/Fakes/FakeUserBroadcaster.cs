using Chat.Application.Abstractions;

namespace Chat.UnitTests.Fakes;

public sealed class FakeUserBroadcaster : IUserBroadcaster
{
	private readonly List<(long UserId, long GuildId, string GuildName)> _calls = [];

	public IReadOnlyList<(long UserId, long GuildId, string GuildName)> Calls => _calls;

	public Task BroadcastGuildJoinedAsync(long userId, long guildId, string guildName, CancellationToken ct)
	{
		_calls.Add((userId, guildId, guildName));
		return Task.CompletedTask;
	}
}
