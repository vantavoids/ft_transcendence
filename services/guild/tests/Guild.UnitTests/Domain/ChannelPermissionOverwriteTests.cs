using Guild.Domain.Guild;
using Guild.Domain.Results;
using Xunit;

namespace Guild.UnitTests.Domain;

public sealed class ChannelPermissionOverwriteTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public void Create_HappyPath()
	{
		var result = ChannelPermissionOverwrite.Create(
			id: 1, guildId: 99, channelId: 10,
			targetType: OverwriteTargetType.Role, targetId: 50,
			allow: (long)Permission.SendMessages,
			deny: (long)Permission.ManageMessages,
			now: Now);

		Assert.True(result.Succeeded);
		Assert.Equal(99L, result.Value.GuildId);
		Assert.Equal(10L, result.Value.ChannelId);
		Assert.Equal(OverwriteTargetType.Role, result.Value.TargetType);
		Assert.Equal(50L, result.Value.TargetId);
		Assert.Equal((long)Permission.SendMessages, result.Value.Allow);
		Assert.Equal((long)Permission.ManageMessages, result.Value.Deny);
	}

	[Fact]
	public void Create_TargetIdNonPositive_Fails()
	{
		var result = ChannelPermissionOverwrite.Create(
			id: 1, guildId: 99, channelId: 10,
			targetType: OverwriteTargetType.Role, targetId: 0,
			allow: 0, deny: 0, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.OverwriteInvalidTarget, result.Error);
	}

	[Fact]
	public void Create_AllowDenyOverlap_Fails()
	{
		long bit = (long)Permission.SendMessages;
		var result = ChannelPermissionOverwrite.Create(
			id: 1, guildId: 99, channelId: 10,
			targetType: OverwriteTargetType.Member, targetId: 42,
			allow: bit, deny: bit, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.OverwriteAllowDenyOverlap, result.Error);
	}

	[Fact]
	public void UpdatePermissions_HappyPath()
	{
		var overwrite = ChannelPermissionOverwrite.Create(
			id: 1, guildId: 99, channelId: 10,
			targetType: OverwriteTargetType.Member, targetId: 42,
			allow: 0, deny: 0, now: Now).Value;

		var later = Now.AddHours(1);
		var result = overwrite.UpdatePermissions(allow: 4L, deny: 8L, now: later);

		Assert.True(result.Succeeded);
		Assert.Equal(4L, overwrite.Allow);
		Assert.Equal(8L, overwrite.Deny);
		Assert.Equal(later, overwrite.UpdatedAt);
	}

	[Fact]
	public void UpdatePermissions_OverlapRejected()
	{
		var overwrite = ChannelPermissionOverwrite.Create(
			id: 1, guildId: 99, channelId: 10,
			targetType: OverwriteTargetType.Role, targetId: 50,
			allow: 0, deny: 0, now: Now).Value;

		var result = overwrite.UpdatePermissions(allow: 3L, deny: 1L, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.OverwriteAllowDenyOverlap, result.Error);
		Assert.Equal(0L, overwrite.Allow);
		Assert.Equal(0L, overwrite.Deny);
	}
}
