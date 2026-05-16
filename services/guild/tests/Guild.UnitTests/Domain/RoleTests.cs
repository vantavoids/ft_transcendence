using System.Reflection;
using Guild.Domain.Guild;
using Xunit;

namespace Guild.UnitTests.Domain;

public sealed class RoleTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public void CreateEveryone_HasCorrectShape()
	{
		var role = InvokeFactory("CreateEveryone", id: 1, guildId: 10, Now);

		Assert.True(role.IsDefault);
		Assert.Equal(515L, role.Permissions);
		Assert.Equal(0, role.Position);
		Assert.Equal("@everyone", role.Name);
		Assert.Equal(10L, role.GuildId);
	}

	[Fact]
	public void CreateAdministrator_HasCorrectShape()
	{
		var role = InvokeFactory("CreateAdministrator", id: 2, guildId: 10, Now);

		Assert.False(role.IsDefault);
		Assert.Equal(256L, role.Permissions);
		Assert.Equal(1, role.Position);
		Assert.Equal("Administrator", role.Name);
		Assert.Equal(10L, role.GuildId);
	}

	// both factories are `internal`. reach them via reflection from the test
	// assembly rather than poking InternalsVisibleTo into Guild.Domain
	// y'know, for science
	private static Role InvokeFactory(string methodName, long id, long guildId, DateTimeOffset now)
	{
		var method = typeof(Role).GetMethod(
			methodName,
			BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!;

		return (Role)method.Invoke(null, [id, guildId, now])!;
	}
}
