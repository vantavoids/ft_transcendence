using Guild.Application.Features.Roles.DeleteRole;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class DeleteRoleHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_Returns_GuildNotFound()
	{
		var (handler, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new DeleteRoleCommand(999, 102));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task NonMember_Returns_NotAMember()
	{
		var (handler, _) = MakeHandler(currentUser: 99);

		var result = await handler.HandleAsync(new DeleteRoleCommand(100, 102));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
	}

	[Fact]
	public async Task MemberWithoutManageRoles_Returns_MissingPermission()
	{
		var (handler, guilds) = MakeHandler(currentUser: 2);
		DomainSeed.AddMember(guilds.Store[100], userId: 2, joinedAt: Now);

		var result = await handler.HandleAsync(new DeleteRoleCommand(100, 102));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.MissingPermission", result.Error.Code);
	}

	[Fact]
	public async Task RoleNotFound_Returns_RoleNotFound()
	{
		var (handler, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new DeleteRoleCommand(100, 999));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.RoleNotFound", result.Error.Code);
	}

	[Fact]
	public async Task DefaultRole_Returns_CannotDeleteDefaultRole()
	{
		var (handler, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new DeleteRoleCommand(100, 101));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.CannotDeleteDefaultRole", result.Error.Code);
	}

	[Fact]
	public async Task CallerDoesNotOutRankRole_Returns_RoleHierarchyBlocked()
	{
		var (handler, guilds) = MakeHandler(currentUser: 2);
		var guild = guilds.Store[100];
		var mod = DomainSeed.AddCustomRole(guild, roleId: 500, name: "Mod",
			permissions: (long)Permission.ManageRoles, position: 3, now: Now);
		var senior = DomainSeed.AddCustomRole(guild, roleId: 501, name: "Senior",
			permissions: 0, position: 5, now: Now);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: mod.Id, now: Now);

		var result = await handler.HandleAsync(new DeleteRoleCommand(100, senior.Id));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.RoleHierarchyBlocked", result.Error.Code);
	}

	[Fact]
	public async Task HappyPath_RemovesRole_AndCascadesMemberRoleAssignments()
	{
		var (handler, guilds) = MakeHandler(currentUser: 1);
		var guild = guilds.Store[100];
		var custom = DomainSeed.AddCustomRole(guild, roleId: 500, name: "Custom",
			permissions: 0, position: 2, now: Now);
		DomainSeed.AddMember(guild, userId: 5, joinedAt: Now);
		DomainSeed.AssignRole(guild, userId: 5, roleId: custom.Id, now: Now);
		Assert.Single(guild.MemberRoles, mr => mr.RoleId == custom.Id);

		var result = await handler.HandleAsync(new DeleteRoleCommand(100, custom.Id));

		Assert.True(result.Succeeded);
		Assert.DoesNotContain(guild.Roles, r => r.Id == custom.Id);
		Assert.DoesNotContain(guild.MemberRoles, mr => mr.RoleId == custom.Id);
	}

	private static (
		Guild.Application.Abstractions.Messaging.ICommandHandler<DeleteRoleCommand, Result> Handler,
		FakeGuildRepository Guilds)
		MakeHandler(long currentUser)
	{
		var guilds = new FakeGuildRepository();
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		guilds.AddAsync(guild).GetAwaiter().GetResult();
		var handler = HandlerFactory.CreateCommand<DeleteRoleCommand, Result>(
			guilds, new FakeClock(Now), new FakeCurrentUser { Id = currentUser });
		return (handler, guilds);
	}
}
