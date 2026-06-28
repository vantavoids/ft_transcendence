using Guild.Application.Features.Roles.Common;
using Guild.Application.Features.Roles.UpdateRole;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class UpdateRoleHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_Returns_GuildNotFound()
	{
		var (handler, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(Patch(guildId: 999, roleId: 102, name: "X"));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task NonMember_Returns_NotAMember()
	{
		var (handler, _) = MakeHandler(currentUser: 99);

		var result = await handler.HandleAsync(Patch(guildId: 100, roleId: 102, name: "X"));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
	}

	[Fact]
	public async Task MemberWithoutManageRoles_Returns_MissingPermission()
	{
		var (handler, guilds) = MakeHandler(currentUser: 2);
		DomainSeed.AddMember(guilds.Store[100], userId: 2, joinedAt: Now);

		var result = await handler.HandleAsync(Patch(guildId: 100, roleId: 102, name: "X"));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.MissingPermission", result.Error.Code);
	}

	[Fact]
	public async Task RoleNotFound_Returns_RoleNotFound()
	{
		var (handler, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(Patch(guildId: 100, roleId: 999, name: "X"));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.RoleNotFound", result.Error.Code);
	}

	[Fact]
	public async Task CallerDoesNotOutRankRole_Returns_RoleHierarchyBlocked()
	{
		// non-owner manager assigned to a custom role at position 3 tries to
		// edit the Administrator role at position 1... actually no, that's
		// out-ranked. Let's flip: caller is at position 3, target at position 5
		var (handler, guilds) = MakeHandler(currentUser: 2);
		var guild = guilds.Store[100];
		var mod = DomainSeed.AddCustomRole(guild, roleId: 500, name: "Mod",
			permissions: (long)Permission.ManageRoles, position: 3, now: Now);
		var senior = DomainSeed.AddCustomRole(guild, roleId: 501, name: "Senior",
			permissions: 0, position: 5, now: Now);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: mod.Id, now: Now);

		var result = await handler.HandleAsync(Patch(guildId: 100, roleId: senior.Id, name: "X"));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.RoleHierarchyBlocked", result.Error.Code);
	}

	[Fact]
	public async Task TryingToGrantPermissionsCallerLacks_Returns_CannotGrantPermissionsYouLack()
	{
		var (handler, guilds) = MakeHandler(currentUser: 2);
		var guild = guilds.Store[100];
		var mr = DomainSeed.AddCustomRole(guild, roleId: 500, name: "RoleManager",
			permissions: (long)Permission.ManageRoles, position: 5, now: Now);
		var lower = DomainSeed.AddCustomRole(guild, roleId: 501, name: "Helper",
			permissions: 0, position: 2, now: Now);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: mr.Id, now: Now);

		var result = await handler.HandleAsync(Patch(guildId: 100, roleId: lower.Id,
			permissions: (long)Permission.BanMembers));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.CannotGrantPermissionsYouLack", result.Error.Code);
	}

	[Fact]
	public async Task DefaultRole_NameChange_Returns_CannotEditDefaultRole()
	{
		// @everyone is at id=101 in our seed (createGuild uses everyoneRoleId: 101)
		var (handler, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(Patch(guildId: 100, roleId: 101, name: "renamed"));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.CannotEditDefaultRole", result.Error.Code);
	}

	[Fact]
	public async Task DefaultRole_PermissionsChange_Succeeds()
	{
		var (handler, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(Patch(guildId: 100, roleId: 101,
			permissions: (long)Permission.SendMessages));

		Assert.True(result.Succeeded);
		Assert.Equal((long)Permission.SendMessages, result.Value.Permissions);
	}

	[Fact]
	public async Task HappyPath_UpdatesNonDefaultRole()
	{
		var (handler, guilds) = MakeHandler(currentUser: 1);
		DomainSeed.AddCustomRole(guilds.Store[100], roleId: 500, name: "Mod",
			permissions: 0, position: 2, now: Now);

		var result = await handler.HandleAsync(Patch(guildId: 100, roleId: 500,
			name: "Senior Mod", color: "#123456", permissions: (long)Permission.KickMembers,
			isHoisted: true, isMentionable: false));

		Assert.True(result.Succeeded);
		Assert.Equal("Senior Mod", result.Value.Name);
		Assert.Equal("#123456", result.Value.Color);
		Assert.Equal((long)Permission.KickMembers, result.Value.Permissions);
		Assert.True(result.Value.IsHoisted);
	}

	private static UpdateRoleCommand Patch(
		long roleId,
		long guildId = 100,
		string? name = null,
		string? color = null,
		long? permissions = null,
		bool? isHoisted = null,
		bool? isMentionable = null)
		=> new(guildId, roleId, name, color, permissions, isHoisted, isMentionable);

	private static (
		Guild.Application.Abstractions.Messaging.ICommandHandler<UpdateRoleCommand, Result<RoleResponse>> Handler,
		FakeGuildRepository Guilds)
		MakeHandler(long currentUser)
	{
		var guilds = new FakeGuildRepository();
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		guilds.Add(guild);
		var handler = HandlerFactory.CreateCommand<UpdateRoleCommand, Result<RoleResponse>>(
			guilds, new FakeClock(Now), new FakeCurrentUser { Id = currentUser });
		return (handler, guilds);
	}
}
