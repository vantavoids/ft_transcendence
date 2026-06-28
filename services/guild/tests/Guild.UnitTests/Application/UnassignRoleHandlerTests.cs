using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Membership.UnassignRole;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class UnassignRoleHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_Returns_GuildNotFound()
	{
		var (handler, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new UnassignRoleCommand(999, TargetUserId: 3, RoleId: 500));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task CallerNotMember_Returns_NotAMember()
	{
		var (handler, _) = MakeHandler(currentUser: 99);

		var result = await handler.HandleAsync(new UnassignRoleCommand(100, TargetUserId: 3, RoleId: 500));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
	}

	[Fact]
	public async Task CallerWithoutManageRoles_Returns_MissingPermission()
	{
		var (handler, guilds) = MakeHandler(currentUser: 2);
		var guild = guilds.Store[100];
		DomainSeed.AddCustomRole(guild, roleId: 500, name: "Role", permissions: 0, position: 5, now: Now);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		DomainSeed.AddMember(guild, userId: 3, joinedAt: Now);
		DomainSeed.AssignRole(guild, userId: 3, roleId: 500, now: Now);

		var result = await handler.HandleAsync(new UnassignRoleCommand(100, TargetUserId: 3, RoleId: 500));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.MissingPermission", result.Error.Code);
	}

	[Fact]
	public async Task UnknownRole_Returns_RoleNotFound()
	{
		var (handler, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new UnassignRoleCommand(100, TargetUserId: 3, RoleId: 999));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.RoleNotFound", result.Error.Code);
	}

	[Fact]
	public async Task RoleNotAssigned_Returns_RoleAssignmentNotFound()
	{
		var (handler, guilds) = MakeHandler(currentUser: 1);
		var guild = guilds.Store[100];
		DomainSeed.AddCustomRole(guild, roleId: 500, name: "Role",
			permissions: (long)Permission.SendMessages, position: 5, now: Now);
		DomainSeed.AddMember(guild, userId: 3, joinedAt: Now);

		var result = await handler.HandleAsync(new UnassignRoleCommand(100, TargetUserId: 3, RoleId: 500));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.RoleAssignmentNotFound", result.Error.Code);
	}

	[Fact]
	public async Task HappyPath_RemovesAssignment()
	{
		var (handler, guilds) = MakeHandler(currentUser: 1);
		var guild = guilds.Store[100];
		DomainSeed.AddCustomRole(guild, roleId: 500, name: "Role",
			permissions: (long)Permission.SendMessages, position: 5, now: Now);
		DomainSeed.AddMember(guild, userId: 3, joinedAt: Now);
		DomainSeed.AssignRole(guild, userId: 3, roleId: 500, now: Now);

		var result = await handler.HandleAsync(new UnassignRoleCommand(100, TargetUserId: 3, RoleId: 500));

		Assert.True(result.Succeeded);
		Assert.DoesNotContain(guild.MemberRoles, mr => mr.UserId == 3 && mr.RoleId == 500);
	}

	private static (ICommandHandler<UnassignRoleCommand, Result> Handler, FakeGuildRepository Guilds)
		MakeHandler(long currentUser)
	{
		var guilds = new FakeGuildRepository();
		var clock = new FakeClock(Now);
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		guilds.AddAsync(guild).GetAwaiter().GetResult();

		var handler = HandlerFactory.CreateCommand<UnassignRoleCommand, Result>(
			guilds, clock, new FakeCurrentUser { Id = currentUser });
		return (handler, guilds);
	}
}
