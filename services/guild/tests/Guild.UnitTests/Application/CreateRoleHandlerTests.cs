using Guild.Application.Features.Roles.Common;
using Guild.Application.Features.Roles.CreateRole;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class CreateRoleHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_Returns_GuildNotFound()
	{
		var (handler, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new CreateRoleCommand(999, "Mod", null, 0, false, false));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task NonMember_Returns_NotAMember()
	{
		var (handler, _) = MakeHandler(currentUser: 99);

		var result = await handler.HandleAsync(new CreateRoleCommand(100, "Mod", null, 0, false, false));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
	}

	[Fact]
	public async Task MemberWithoutManageRoles_Returns_MissingPermission()
	{
		var (handler, guilds) = MakeHandler(currentUser: 2);
		DomainSeed.AddMember(guilds.Store[100], userId: 2, joinedAt: Now);

		var result = await handler.HandleAsync(new CreateRoleCommand(100, "Mod", null, 0, false, false));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.MissingPermission", result.Error.Code);
	}

	[Fact]
	public async Task AttemptingToGrantPermissionsCallerLacks_Returns_CannotGrantPermissionsYouLack()
	{
		var (handler, guilds) = MakeHandler(currentUser: 2);
		var guild = guilds.Store[100];
		var mr = DomainSeed.AddCustomRole(guild, roleId: 500, name: "RoleManager",
			permissions: (long)Permission.ManageRoles, position: 5, now: Now);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: mr.Id, now: Now);

		// caller only has MANAGE_ROLES, tries to grant BAN_MEMBERS
		var result = await handler.HandleAsync(new CreateRoleCommand(
			GuildId: 100, Name: "Mod",
			Color: null, Permissions: (long)Permission.BanMembers,
			IsHoisted: false, IsMentionable: false));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.CannotGrantPermissionsYouLack", result.Error.Code);
	}

	[Fact]
	public async Task InvalidName_Returns_InvalidRoleName()
	{
		var (handler, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new CreateRoleCommand(100, "", null, 0, false, false));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.InvalidRoleName", result.Error.Code);
	}

	[Fact]
	public async Task InvalidColor_Returns_InvalidColor()
	{
		var (handler, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new CreateRoleCommand(100, "Mod", "not-a-hex", 0, false, false));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.InvalidColor", result.Error.Code);
	}

	[Fact]
	public async Task HappyPath_AddsRole_AtMaxPositionPlusOne_AsNonDefault()
	{
		var (handler, guilds) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new CreateRoleCommand(
			GuildId: 100, Name: "Moderator",
			Color: "#FF5733", Permissions: (long)Permission.KickMembers,
			IsHoisted: true, IsMentionable: false));

		Assert.True(result.Succeeded);
		Assert.Equal("Moderator", result.Value.Name);
		Assert.Equal("#FF5733", result.Value.Color);
		Assert.Equal((long)Permission.KickMembers, result.Value.Permissions);
		Assert.False(result.Value.IsDefault);
		Assert.True(result.Value.IsHoisted);
		// pre-existing roles are @everyone(0) + Administrator(1), so new role at 2
		Assert.Equal(2, result.Value.Position);

		// aggregate state reflects the new role
		var stored = guilds.Store[100].Roles.Single(r => r.Name == "Moderator");
		Assert.Equal(2, stored.Position);
	}

	private static (
		Guild.Application.Abstractions.Messaging.ICommandHandler<CreateRoleCommand, Result<RoleResponse>> Handler,
		FakeGuildRepository Guilds)
		MakeHandler(long currentUser)
	{
		var guilds = new FakeGuildRepository();
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		guilds.Add(guild);
		var handler = HandlerFactory.CreateCommand<CreateRoleCommand, Result<RoleResponse>>(
			guilds, new FakeIdGenerator(seed: 1000), new FakeClock(Now),
			new FakeCurrentUser { Id = currentUser });
		return (handler, guilds);
	}
}
