using Guild.Domain.Results;

namespace Guild.Domain.Guild;

public sealed class GuildMember
{
	public const int MaxNicknameLen = 64;

	// EF Core constructor
	private GuildMember() { }

	private GuildMember(long guildId, long userId, string? nickname, DateTimeOffset joinedAt)
	{
		GuildId = guildId;
		UserId = userId;
		Nickname = nickname;
		JoinedAt = joinedAt;
	}

	public long GuildId { get; private set; }
	public long UserId { get; private set; }
	public string? Nickname { get; private set; }
	public DateTimeOffset JoinedAt { get; private set; }

	public static Result<GuildMember> Create(long guildId, long userId, DateTimeOffset now)
	{
		if (guildId <= 0 || userId <= 0)
			return GuildFailures.TargetNotAMember;

		return new GuildMember(guildId, userId, nickname: null, joinedAt: now);
	}
}
