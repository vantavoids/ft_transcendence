using Guild.Domain.Guild;
using Xunit;

namespace Guild.UnitTests.Domain;

public sealed class GuildBanTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public void Create_WithValidArgs_SetsAllFields()
	{
		var result = GuildBan.Create(guildId: 100, userId: 3, bannedBy: 1, reason: "spam", now: Now);

		Assert.True(result.Succeeded);
		var ban = result.Value;
		Assert.Equal(100, ban.GuildId);
		Assert.Equal(3, ban.UserId);
		Assert.Equal(1, ban.BannedBy);
		Assert.Equal("spam", ban.Reason);
		Assert.Equal(Now, ban.BannedAt);
	}

	[Fact]
	public void Create_WithNullReason_Succeeds()
	{
		var result = GuildBan.Create(guildId: 100, userId: 3, bannedBy: 1, reason: null, now: Now);

		Assert.True(result.Succeeded);
		Assert.Null(result.Value.Reason);
	}

	[Fact]
	public void Create_WithReasonAtMaxLen_Succeeds()
	{
		var reason = new string('x', GuildBan.MaxReasonLen);

		var result = GuildBan.Create(guildId: 100, userId: 3, bannedBy: 1, reason: reason, now: Now);

		Assert.True(result.Succeeded);
		Assert.Equal(GuildBan.MaxReasonLen, result.Value.Reason!.Length);
	}

	[Fact]
	public void Create_WithReasonOverMaxLen_Returns_BanReasonTooLong()
	{
		var reason = new string('x', GuildBan.MaxReasonLen + 1);

		var result = GuildBan.Create(guildId: 100, userId: 3, bannedBy: 1, reason: reason, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.BanReasonTooLong", result.Error.Code);
	}

	[Theory]
	[InlineData(0, 3, 1)]
	[InlineData(-1, 3, 1)]
	[InlineData(100, 0, 1)]
	[InlineData(100, -1, 1)]
	[InlineData(100, 3, 0)]
	[InlineData(100, 3, -1)]
	public void Create_WithNonPositiveId_Returns_InvalidId(long guildId, long userId, long bannedBy)
	{
		var result = GuildBan.Create(guildId, userId, bannedBy, reason: null, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.InvalidId", result.Error.Code);
	}
}
