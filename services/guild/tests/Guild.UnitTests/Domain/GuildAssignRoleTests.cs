using Guild.Domain.Guild;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Domain;

public sealed class GuildAssignRoleTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	// guild seeded by Create with @everyone(101, default) + Administrator(102),
	// owner 1, plus member 2 and a custom non-default role 500
	private static GuildEntity MakeGuild()
	{
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		DomainSeed.AddCustomRole(guild, roleId: 500, name: "Mod", permissions: 0, position: 2, now: Now);
		return guild;
	}

	[Fact]
	public void TargetNotAMember_Returns_TargetNotAMember()
	{
		var guild = MakeGuild();

		var result = guild.AssignRole(userId: 555, roleId: 500, Now);

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.TargetNotAMember", result.Error.Code);
	}

	[Fact]
	public void UnknownRole_Returns_RoleNotFound()
	{
		var guild = MakeGuild();

		var result = guild.AssignRole(userId: 2, roleId: 999, Now);

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.RoleNotFound", result.Error.Code);
	}

	[Fact]
	public void DefaultRole_Returns_CannotAssignDefaultRole()
	{
		var guild = MakeGuild();

		var result = guild.AssignRole(userId: 2, roleId: 101, Now); // @everyone

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.CannotAssignDefaultRole", result.Error.Code);
	}

	[Fact]
	public void AlreadyAssigned_IsIdempotent_NoDuplicate()
	{
		var guild = MakeGuild();
		DomainSeed.AssignRole(guild, userId: 2, roleId: 500, now: Now);

		var result = guild.AssignRole(userId: 2, roleId: 500, Now);

		Assert.True(result.Succeeded);
		Assert.Single(guild.MemberRoles, mr => mr.UserId == 2 && mr.RoleId == 500);
	}

	[Fact]
	public void HappyPath_AddsMemberRole()
	{
		var guild = MakeGuild();

		var result = guild.AssignRole(userId: 2, roleId: 500, Now);

		Assert.True(result.Succeeded);
		Assert.Contains(guild.MemberRoles, mr => mr.UserId == 2 && mr.RoleId == 500);
	}
}
