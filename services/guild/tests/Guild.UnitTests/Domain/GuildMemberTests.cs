using Guild.Domain.Guild;
using Guild.Domain.Results;
using Xunit;

namespace Guild.UnitTests.Domain;

public sealed class GuildMemberTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public void Create_HappyPath()
	{
		var result = GuildMember.Create(guildId: 10, userId: 42, now: Now);

		Assert.True(result.Succeeded);
		var member = result.Value;

		Assert.Equal(10L, member.GuildId);
		Assert.Equal(42L, member.UserId);
		Assert.Equal(Now, member.JoinedAt);
	}

	[Fact]
	public void Create_NullableNicknameDefaultsNull()
	{
		var member = GuildMember.Create(guildId: 10, userId: 42, now: Now).Value;

		Assert.Null(member.Nickname);
	}

	[Fact]
	public void Create_ZeroGuildId_Fails()
	{
		var result = GuildMember.Create(guildId: 0, userId: 42, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.TargetNotAMember, result.Error);
	}

	[Fact]
	public void Create_NegativeGuildId_Fails()
	{
		var result = GuildMember.Create(guildId: -1, userId: 42, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.TargetNotAMember, result.Error);
	}

	[Fact]
	public void Create_ZeroUserId_Fails()
	{
		var result = GuildMember.Create(guildId: 10, userId: 0, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.TargetNotAMember, result.Error);
	}

	[Fact]
	public void Create_NegativeUserId_Fails()
	{
		var result = GuildMember.Create(guildId: 10, userId: -1, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.TargetNotAMember, result.Error);
	}
}
