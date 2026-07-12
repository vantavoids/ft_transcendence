using Guild.Application.Features.Roles.ListRoles;
using Guild.Application.Features.Roles.ReorderRoles;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class ReorderRolesHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_Returns_GuildNotFound()
	{
		var (handler, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(Cmd(guildId: 999, (102, 1)));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task NonMember_Returns_NotAMember()
	{
		var (handler, _) = MakeHandler(currentUser: 99);

		var result = await handler.HandleAsync(Cmd(guildId: 100, (102, 1)));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
	}

	[Fact]
	public async Task MemberWithoutManageRoles_Returns_MissingPermission()
	{
		var (handler, guilds) = MakeHandler(currentUser: 2);
		DomainSeed.AddMember(guilds.Store[100], userId: 2, joinedAt: Now);

		var result = await handler.HandleAsync(Cmd(guildId: 100, (102, 1)));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.MissingPermission", result.Error.Code);
	}

	[Fact]
	public async Task Owner_SwapSubset_Succeeds_AndReturnsDescending()
	{
		var (handler, guilds) = MakeHandler(currentUser: 1);
		var guild = guilds.Store[100];
		DomainSeed.AddCustomRole(guild, roleId: 500, name: "A", permissions: 0, position: 2, now: Now);
		DomainSeed.AddCustomRole(guild, roleId: 501, name: "B", permissions: 0, position: 3, now: Now);

		// swap A and B; admin (pos 1) untouched and not in the body
		var result = await handler.HandleAsync(Cmd(guildId: 100, (500, 3), (501, 2)));

		Assert.True(result.Succeeded);
		var items = result.Value.Items;
		Assert.Equal(new[] { "500", "501", "102", "101" }, items.Select(i => i.Id).ToArray());
		Assert.Equal(3, items.Single(i => i.Id == "500").Position);
		Assert.Equal(0, items[^1].Position);
	}

	[Fact]
	public async Task NonOwner_ReorderingRolesBelowSelf_Succeeds()
	{
		// caller's highest role is Top at position 4; they swap two roles below
		// them and need not include their own role or admin in the body
		var (handler, guilds) = MakeHandler(currentUser: 2);
		var guild = guilds.Store[100];
		var top = DomainSeed.AddCustomRole(guild, roleId: 500, name: "Top",
			permissions: (long)Permission.ManageRoles, position: 4, now: Now);
		DomainSeed.AddCustomRole(guild, roleId: 501, name: "A", permissions: 0, position: 2, now: Now);
		DomainSeed.AddCustomRole(guild, roleId: 502, name: "B", permissions: 0, position: 3, now: Now);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: top.Id, now: Now);

		var result = await handler.HandleAsync(Cmd(guildId: 100, (501, 3), (502, 2)));

		Assert.True(result.Succeeded);
		Assert.Equal(3, result.Value.Items.Single(i => i.Id == "501").Position);
		Assert.Equal(2, result.Value.Items.Single(i => i.Id == "502").Position);
		Assert.Equal(4, result.Value.Items.Single(i => i.Id == "500").Position); // unchanged
	}

	[Fact]
	public async Task NonOwner_MovingOwnHighestRole_Returns_RoleHierarchyBlocked()
	{
		// caller's highest role sits at position 4 (== their rank). swapping it
		// with the role below is blocked: the caller does not strictly out-rank
		// their own role
		var (handler, guilds) = MakeHandler(currentUser: 2);
		var guild = guilds.Store[100];
		var top = DomainSeed.AddCustomRole(guild, roleId: 500, name: "Top",
			permissions: (long)Permission.ManageRoles, position: 4, now: Now);
		DomainSeed.AddCustomRole(guild, roleId: 502, name: "B", permissions: 0, position: 3, now: Now);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: top.Id, now: Now);

		// Top(4) <-> B(3): a valid permutation of {3,4}, but Top is the caller's own role
		var result = await handler.HandleAsync(Cmd(guildId: 100, (500, 3), (502, 4)));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.RoleHierarchyBlocked", result.Error.Code);
	}

	[Fact]
	public async Task NotAPermutation_Returns_RoleReorderInvalidPositions()
	{
		// owner (passes hierarchy) but target slot is not occupied by a body role
		var (handler, guilds) = MakeHandler(currentUser: 1);
		var guild = guilds.Store[100];
		DomainSeed.AddCustomRole(guild, roleId: 500, name: "A", permissions: 0, position: 2, now: Now);

		var result = await handler.HandleAsync(Cmd(guildId: 100, (500, 5)));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.RoleReorderInvalidPositions", result.Error.Code);
	}

	private static ReorderRolesCommand Cmd(long guildId, params (long RoleId, int Position)[] moves)
		=> new(guildId, moves.Select(m => new RolePositionEntry(m.RoleId, m.Position)).ToList());

	private static (
		Guild.Application.Abstractions.Messaging.ICommandHandler<ReorderRolesCommand, Result<RoleListResponse>> Handler,
		FakeGuildRepository Guilds)
		MakeHandler(long currentUser)
	{
		var guilds = new FakeGuildRepository();
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		guilds.Add(guild);
		var handler = HandlerFactory.CreateCommand<ReorderRolesCommand, Result<RoleListResponse>>(
			guilds, new FakeEventBus(), new FakeClock(Now), new FakeCurrentUser { Id = currentUser });
		return (handler, guilds);
	}
}
