using Guild.Application.Contracts;
using Guild.Application.Features.Membership.KickMember;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class KickMemberHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_Returns_GuildNotFound()
	{
		var (handler, _, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new KickMemberCommand(999, TargetUserId: 2));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task CallerNotMember_Returns_NotAMember()
	{
		var (handler, _, _) = MakeHandler(currentUser: 99);

		var result = await handler.HandleAsync(new KickMemberCommand(100, TargetUserId: 1));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
	}

	[Fact]
	public async Task TargetNotAMember_Returns_TargetNotAMember()
	{
		var (handler, _, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new KickMemberCommand(100, TargetUserId: 555));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.TargetNotAMember", result.Error.Code);
	}

	[Fact]
	public async Task CallerWithoutKickMembers_Returns_MissingPermission()
	{
		var (handler, guilds, _) = MakeHandler(currentUser: 2);
		DomainSeed.AddMember(guilds.Store[100], userId: 2, joinedAt: Now);
		DomainSeed.AddMember(guilds.Store[100], userId: 3, joinedAt: Now);

		var result = await handler.HandleAsync(new KickMemberCommand(100, TargetUserId: 3));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.MissingPermission", result.Error.Code);
	}

	[Fact]
	public async Task OwnerAsTarget_Returns_CannotKickOwner()
	{
		// caller is a non-owner with KICK_MEMBERS perm but target is the owner
		var (handler, guilds, _) = MakeHandler(currentUser: 2);
		var guild = guilds.Store[100];
		var mod = DomainSeed.AddCustomRole(guild, roleId: 500, name: "Mod",
			permissions: (long)Permission.KickMembers, position: 5, now: Now);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: mod.Id, now: Now);

		var result = await handler.HandleAsync(new KickMemberCommand(100, TargetUserId: 1));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.CannotKickOwner", result.Error.Code);
	}

	[Fact]
	public async Task SameRank_Returns_RoleHierarchyBlocked()
	{
		var (handler, guilds, _) = MakeHandler(currentUser: 2);
		var guild = guilds.Store[100];
		var mod = DomainSeed.AddCustomRole(guild, roleId: 500, name: "Mod",
			permissions: (long)Permission.KickMembers, position: 5, now: Now);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: mod.Id, now: Now);
		DomainSeed.AddMember(guild, userId: 3, joinedAt: Now);
		DomainSeed.AssignRole(guild, userId: 3, roleId: mod.Id, now: Now);

		var result = await handler.HandleAsync(new KickMemberCommand(100, TargetUserId: 3));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.RoleHierarchyBlocked", result.Error.Code);
	}

	[Fact]
	public async Task SelfKick_Returns_RoleHierarchyBlocked()
	{
		// caller has KICK_MEMBERS but tries to kick themselves; never out-ranks self
		var (handler, guilds, _) = MakeHandler(currentUser: 2);
		var guild = guilds.Store[100];
		var mod = DomainSeed.AddCustomRole(guild, roleId: 500, name: "Mod",
			permissions: (long)Permission.KickMembers, position: 5, now: Now);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: mod.Id, now: Now);

		var result = await handler.HandleAsync(new KickMemberCommand(100, TargetUserId: 2));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.RoleHierarchyBlocked", result.Error.Code);
	}

	[Fact]
	public async Task HappyPath_RemovesMember_AndPublishesGuildMemberLeft()
	{
		var (handler, guilds, events) = MakeHandler(currentUser: 2);
		var guild = guilds.Store[100];
		var mod = DomainSeed.AddCustomRole(guild, roleId: 500, name: "Mod",
			permissions: (long)Permission.KickMembers, position: 5, now: Now);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: mod.Id, now: Now);
		DomainSeed.AddMember(guild, userId: 3, joinedAt: Now); // bottom rank

		var result = await handler.HandleAsync(new KickMemberCommand(100, TargetUserId: 3));

		Assert.True(result.Succeeded);
		Assert.DoesNotContain(guild.Members, m => m.UserId == 3);
		var ev = events.Single<GuildMemberLeft>();
		Assert.Equal(100L, ev.GuildId);
		Assert.Equal(3L, ev.UserId);
	}

	private static (
		Guild.Application.Abstractions.Messaging.ICommandHandler<KickMemberCommand, Result> Handler,
		FakeGuildRepository Guilds,
		FakeEventBus Events)
		MakeHandler(long currentUser)
	{
		var guilds = new FakeGuildRepository();
		var events = new FakeEventBus();
		var clock = new FakeClock(Now);
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		guilds.AddAsync(guild).GetAwaiter().GetResult();

		var handler = HandlerFactory.CreateCommand<KickMemberCommand, Result>(
			guilds, events, clock, new FakeCurrentUser { Id = currentUser });
		return (handler, guilds, events);
	}
}
