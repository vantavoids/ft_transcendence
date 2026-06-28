using Guild.Application.Abstractions.Messaging;
using Guild.Application.Contracts;
using Guild.Application.Features.Bans.BanMember;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class BanMemberHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_Returns_GuildNotFound()
	{
		var (handler, _, _, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new BanMemberCommand(999, TargetUserId: 2, Reason: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task CallerNotMember_Returns_NotAMember()
	{
		var (handler, _, _, _) = MakeHandler(currentUser: 99);

		var result = await handler.HandleAsync(new BanMemberCommand(100, TargetUserId: 2, Reason: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
	}

	[Fact]
	public async Task CallerWithoutBanMembers_Returns_MissingPermission()
	{
		var (handler, guilds, _, _) = MakeHandler(currentUser: 2);
		DomainSeed.AddMember(guilds.Store[100], userId: 2, joinedAt: Now);
		DomainSeed.AddMember(guilds.Store[100], userId: 3, joinedAt: Now);

		var result = await handler.HandleAsync(new BanMemberCommand(100, TargetUserId: 3, Reason: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.MissingPermission", result.Error.Code);
	}

	[Fact]
	public async Task SelfBan_Returns_CannotBanSelf()
	{
		// owner has BAN_MEMBERS via Administrator; the self check precedes the
		// owner check, so banning yourself surfaces CannotBanSelf even as owner
		var (handler, _, _, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new BanMemberCommand(100, TargetUserId: 1, Reason: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.CannotBanSelf", result.Error.Code);
	}

	[Fact]
	public async Task OwnerAsTarget_Returns_CannotBanOwner()
	{
		// caller is a non-owner mod with BAN_MEMBERS, target is the owner
		var (handler, guilds, _, _) = MakeHandler(currentUser: 2);
		var guild = guilds.Store[100];
		var mod = DomainSeed.AddCustomRole(guild, roleId: 500, name: "Mod",
			permissions: (long)Permission.BanMembers, position: 5, now: Now);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: mod.Id, now: Now);

		var result = await handler.HandleAsync(new BanMemberCommand(100, TargetUserId: 1, Reason: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.CannotBanOwner", result.Error.Code);
	}

	[Fact]
	public async Task SameRank_Returns_RoleHierarchyBlocked()
	{
		var (handler, guilds, _, _) = MakeHandler(currentUser: 2);
		var guild = guilds.Store[100];
		var mod = DomainSeed.AddCustomRole(guild, roleId: 500, name: "Mod",
			permissions: (long)Permission.BanMembers, position: 5, now: Now);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: mod.Id, now: Now);
		DomainSeed.AddMember(guild, userId: 3, joinedAt: Now);
		DomainSeed.AssignRole(guild, userId: 3, roleId: mod.Id, now: Now);

		var result = await handler.HandleAsync(new BanMemberCommand(100, TargetUserId: 3, Reason: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.RoleHierarchyBlocked", result.Error.Code);
	}

	[Fact]
	public async Task AlreadyBanned_Returns_AlreadyBanned()
	{
		var (handler, guilds, bans, _) = MakeHandler(currentUser: 1);
		DomainSeed.AddMember(guilds.Store[100], userId: 3, joinedAt: Now);
		bans.Seed(GuildBan.Create(guildId: 100, userId: 3, bannedBy: 1, reason: null, now: Now).Value);

		var result = await handler.HandleAsync(new BanMemberCommand(100, TargetUserId: 3, Reason: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.AlreadyBanned", result.Error.Code);
	}

	[Fact]
	public async Task ReasonTooLong_Returns_BanReasonTooLong()
	{
		var (handler, guilds, bans, _) = MakeHandler(currentUser: 1);
		DomainSeed.AddMember(guilds.Store[100], userId: 3, joinedAt: Now);
		var reason = new string('x', GuildBan.MaxReasonLen + 1);

		var result = await handler.HandleAsync(new BanMemberCommand(100, TargetUserId: 3, Reason: reason));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.BanReasonTooLong", result.Error.Code);
		Assert.Empty(bans.Store);
	}

	[Fact]
	public async Task HappyPath_Member_BansAndRemovesMember_AndPublishesGuildMemberLeft()
	{
		var (handler, guilds, bans, events) = MakeHandler(currentUser: 1);
		var guild = guilds.Store[100];
		DomainSeed.AddMember(guild, userId: 3, joinedAt: Now);

		var result = await handler.HandleAsync(new BanMemberCommand(100, TargetUserId: 3, Reason: "spam"));

		Assert.True(result.Succeeded);
		Assert.True(bans.Store.ContainsKey((100, 3)));
		Assert.DoesNotContain(guild.Members, m => m.UserId == 3);
		var ev = events.Single<GuildMemberLeft>();
		Assert.Equal(100L, ev.GuildId);
		Assert.Equal(3L, ev.UserId);
	}

	[Fact]
	public async Task HappyPath_PreEmptive_BansNonMember_WithoutPublishingLeft()
	{
		// pre-emptive ban: target has never been a member, so no member is
		// removed and no GuildMemberLeft is published
		var (handler, guilds, bans, events) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new BanMemberCommand(100, TargetUserId: 555, Reason: null));

		Assert.True(result.Succeeded);
		Assert.True(bans.Store.ContainsKey((100, 555)));
		Assert.False(events.Has<GuildMemberLeft>());
	}

	private static (
		ICommandHandler<BanMemberCommand, Result> Handler,
		FakeGuildRepository Guilds,
		FakeGuildBanRepository Bans,
		FakeEventBus Events)
		MakeHandler(long currentUser)
	{
		var guilds = new FakeGuildRepository();
		var bans = new FakeGuildBanRepository();
		var events = new FakeEventBus();
		var clock = new FakeClock(Now);
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		guilds.AddAsync(guild).GetAwaiter().GetResult();

		var handler = HandlerFactory.CreateCommand<BanMemberCommand, Result>(
			guilds, bans, events, clock, new FakeCurrentUser { Id = currentUser });
		return (handler, guilds, bans, events);
	}
}
