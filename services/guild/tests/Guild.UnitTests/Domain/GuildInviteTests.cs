using Guild.Domain.Guild;
using Guild.Domain.Results;
using Xunit;

namespace Guild.UnitTests.Domain;

public sealed class GuildInviteTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public void Create_HappyPath()
	{
		var result = GuildInvite.Create(
			code: "abc123",
			guildId: 10,
			createdBy: 42,
			maxUses: 5,
			expiresAt: Now.AddHours(1),
			now: Now);

		Assert.True(result.Succeeded);
		var invite = result.Value;
		Assert.Equal("abc123", invite.Code);
		Assert.Equal(10L, invite.GuildId);
		Assert.Equal(42L, invite.CreatedBy);
		Assert.Equal(5, invite.MaxUses);
		Assert.Equal(0, invite.Uses);
		Assert.False(invite.IsRevoked);
		Assert.Equal(Now.AddHours(1), invite.ExpiresAt);
	}

	[Fact]
	public void Create_NullMaxUses_AllowsUnlimitedUses()
	{
		var result = GuildInvite.Create(
			code: "abc", guildId: 1, createdBy: 1, maxUses: null, expiresAt: null, now: Now);

		Assert.True(result.Succeeded);
		Assert.Null(result.Value.MaxUses);
	}

	[Fact]
	public void Create_EmptyCode_Fails()
	{
		var result = GuildInvite.Create(
			code: "", guildId: 1, createdBy: 1, maxUses: null, expiresAt: null, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.InviteCodeInvalid, result.Error);
	}

	[Fact]
	public void Create_CodeTooLong_Fails()
	{
		var code = new string('a', GuildInvite.MaxCodeLen + 1);

		var result = GuildInvite.Create(
			code: code, guildId: 1, createdBy: 1, maxUses: null, expiresAt: null, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.InviteCodeInvalid, result.Error);
	}

	[Fact]
	public void Create_ZeroGuildId_Fails()
	{
		var result = GuildInvite.Create(
			code: "abc", guildId: 0, createdBy: 1, maxUses: null, expiresAt: null, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.InviteCodeInvalid, result.Error);
	}

	[Fact]
	public void Create_ZeroCreatedBy_Fails()
	{
		var result = GuildInvite.Create(
			code: "abc", guildId: 1, createdBy: 0, maxUses: null, expiresAt: null, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.InviteCodeInvalid, result.Error);
	}

	[Fact]
	public void Create_ZeroMaxUses_Fails()
	{
		var result = GuildInvite.Create(
			code: "abc", guildId: 1, createdBy: 1, maxUses: 0, expiresAt: null, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.InviteMaxUsesInvalid, result.Error);
	}

	[Fact]
	public void Create_NegativeMaxUses_Fails()
	{
		var result = GuildInvite.Create(
			code: "abc", guildId: 1, createdBy: 1, maxUses: -1, expiresAt: null, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.InviteMaxUsesInvalid, result.Error);
	}

	[Fact]
	public void Create_ExpiresInPast_Fails()
	{
		var result = GuildInvite.Create(
			code: "abc", guildId: 1, createdBy: 1, maxUses: null,
			expiresAt: Now.AddHours(-1), now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.InviteExpiresInPast, result.Error);
	}

	[Fact]
	public void Create_ExpiresExactlyNow_Fails()
	{
		var result = GuildInvite.Create(
			code: "abc", guildId: 1, createdBy: 1, maxUses: null, expiresAt: Now, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.InviteExpiresInPast, result.Error);
	}

	[Fact]
	public void Consume_IncrementsUses()
	{
		var invite = CreateValid(maxUses: 5);

		var result = invite.Consume(Now);

		Assert.True(result.Succeeded);
		Assert.Equal(1, invite.Uses);
	}

	[Fact]
	public void Consume_AtMaxUses_Fails()
	{
		var invite = CreateValid(maxUses: 1);
		invite.Consume(Now);

		var result = invite.Consume(Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.InviteUnusable, result.Error);
		Assert.Equal(1, invite.Uses);
	}

	[Fact]
	public void Consume_Revoked_Fails()
	{
		var invite = CreateValid();
		invite.Revoke();

		var result = invite.Consume(Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.InviteUnusable, result.Error);
		Assert.Equal(0, invite.Uses);
	}

	[Fact]
	public void Consume_Expired_Fails()
	{
		var invite = CreateValid(expiresAt: Now.AddHours(1));

		var result = invite.Consume(Now.AddHours(2));

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.InviteUnusable, result.Error);
	}

	[Fact]
	public void Consume_UnlimitedUses_NeverFailsOnCount()
	{
		var invite = CreateValid(maxUses: null);
		for (var i = 0; i < 100; i++)
		{
			var result = invite.Consume(Now);
			Assert.True(result.Succeeded);
		}
		Assert.Equal(100, invite.Uses);
	}

	[Fact]
	public void Revoke_FlagsInvite()
	{
		var invite = CreateValid();

		var result = invite.Revoke();

		Assert.True(result.Succeeded);
		Assert.True(invite.IsRevoked);
	}

	[Fact]
	public void Revoke_AlreadyRevoked_Fails()
	{
		var invite = CreateValid();
		invite.Revoke();

		var result = invite.Revoke();

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.InviteAlreadyRevoked, result.Error);
	}

	[Fact]
	public void IsActive_RevokedFalse()
	{
		var invite = CreateValid();
		invite.Revoke();

		Assert.False(invite.IsActive(Now));
	}

	[Fact]
	public void IsActive_ExpiredFalse()
	{
		var invite = CreateValid(expiresAt: Now.AddHours(1));

		Assert.False(invite.IsActive(Now.AddHours(2)));
	}

	[Fact]
	public void IsActive_ExhaustedFalse()
	{
		var invite = CreateValid(maxUses: 1);
		invite.Consume(Now);

		Assert.False(invite.IsActive(Now));
	}

	[Fact]
	public void IsActive_FreshTrue()
	{
		var invite = CreateValid(maxUses: 5, expiresAt: Now.AddHours(1));

		Assert.True(invite.IsActive(Now));
	}

	private static GuildInvite CreateValid(
		int? maxUses = null,
		DateTimeOffset? expiresAt = null) =>
		GuildInvite.Create(
			code: "abc",
			guildId: 1,
			createdBy: 1,
			maxUses: maxUses,
			expiresAt: expiresAt,
			now: Now).Value;
}
