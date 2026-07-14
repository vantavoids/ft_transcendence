using Guild.Application.Contracts;
using Guild.Application.Features.Users.PurgeUserData;
using Guild.Domain.Guild;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class PurgeUserDataHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	private const long DeletedUser = 42;
	private const long OtherUser = 7;

	[Fact]
	public async Task Purge_InvokesEveryCascadeStep_ForTheDeletedUser()
	{
		var guilds = new FakeGuildRepository();
		var bans = new FakeGuildBanRepository();
		var invites = new FakeGuildInviteRepository();
		var overwrites = new FakeChannelPermissionOverwriteRepository();
		var handler = HandlerFactory.CreateCommand<PurgeUserDataCommand>(
			guilds, bans, invites, overwrites, new FakeEventBus());

		await handler.HandleAsync(new PurgeUserDataCommand(DeletedUser));

		Assert.Equal(1, guilds.PurgeMembershipCount);
		Assert.Equal(DeletedUser, guilds.LastPurgedMember);
		Assert.Equal(1, bans.RemoveAllForUserCount);
		Assert.Equal(1, invites.RemoveAllByCreatorCount);
		Assert.Equal(1, overwrites.RemoveAllForMemberCount);
		Assert.Equal(1, bans.ScrubModeratorCount);
		Assert.Equal(DeletedUser, bans.LastScrubbedModerator);
	}

	[Fact]
	public async Task Purge_RemovesTheUsersInvites_KeepsOthers()
	{
		var invites = new FakeGuildInviteRepository();
		invites.Seed(GuildInvite.Create("MINE01", guildId: 100, createdBy: DeletedUser, maxUses: null, expiresAt: null, now: Now).Value);
		invites.Seed(GuildInvite.Create("THEIRS", guildId: 100, createdBy: OtherUser, maxUses: null, expiresAt: null, now: Now).Value);

		var handler = HandlerFactory.CreateCommand<PurgeUserDataCommand>(
			new FakeGuildRepository(), new FakeGuildBanRepository(), invites,
			new FakeChannelPermissionOverwriteRepository(), new FakeEventBus());

		await handler.HandleAsync(new PurgeUserDataCommand(DeletedUser));

		Assert.False(invites.Store.ContainsKey("MINE01"));
		Assert.True(invites.Store.ContainsKey("THEIRS"));
	}

	[Fact]
	public async Task Purge_RemovesUsersMemberOverwrites_KeepsRoleOverwriteWithSameId_AndOtherUsers()
	{
		var overwrites = new FakeChannelPermissionOverwriteRepository();
		// member overwrite for the deleted user -> removed
		overwrites.Seed(ChannelPermissionOverwrite.Create(1, 100, 200, OverwriteTargetType.Member, DeletedUser, 0, 0, Now).Value);
		// member overwrite for another user -> kept
		overwrites.Seed(ChannelPermissionOverwrite.Create(2, 100, 200, OverwriteTargetType.Member, OtherUser, 0, 0, Now).Value);
		// role overwrite whose target_id equals the deleted user's id -> kept (type guard)
		overwrites.Seed(ChannelPermissionOverwrite.Create(3, 100, 200, OverwriteTargetType.Role, DeletedUser, 0, 0, Now).Value);

		var handler = HandlerFactory.CreateCommand<PurgeUserDataCommand>(
			new FakeGuildRepository(), new FakeGuildBanRepository(), new FakeGuildInviteRepository(),
			overwrites, new FakeEventBus());

		await handler.HandleAsync(new PurgeUserDataCommand(DeletedUser));

		Assert.False(overwrites.Store.ContainsKey(1)); // member overwrite gone
		Assert.True(overwrites.Store.ContainsKey(2));   // other user's kept
		Assert.True(overwrites.Store.ContainsKey(3));   // role overwrite kept despite shared id
	}

	[Fact]
	public async Task Purge_RemovesBansAgainstUser_KeepsBansTheyIssued()
	{
		var bans = new FakeGuildBanRepository();
		// ban against the deleted user -> removed
		bans.Seed(GuildBan.Create(guildId: 100, userId: DeletedUser, bannedBy: OtherUser, reason: null, now: Now).Value);
		// ban the deleted user issued against someone else -> kept (banned_by scrubbed by the real repo)
		bans.Seed(GuildBan.Create(guildId: 100, userId: OtherUser, bannedBy: DeletedUser, reason: null, now: Now).Value);

		var handler = HandlerFactory.CreateCommand<PurgeUserDataCommand>(
			new FakeGuildRepository(), bans, new FakeGuildInviteRepository(),
			new FakeChannelPermissionOverwriteRepository(), new FakeEventBus());

		await handler.HandleAsync(new PurgeUserDataCommand(DeletedUser));

		Assert.False(bans.Store.ContainsKey((100, DeletedUser))); // ban against them gone
		Assert.True(bans.Store.ContainsKey((100, OtherUser)));     // ban they issued stays in force
		Assert.Equal(DeletedUser, bans.LastScrubbedModerator);     // and their moderator ref was scrubbed
	}

	[Fact]
	public async Task Purge_EmitsMemberLeft_ForEachGuildTheUserBelongedTo()
	{
		var guilds = new FakeGuildRepository();
		var inGuildA = GuildEntity.Create(
			id: 100, name: "A", description: null, iconUrl: null, bannerUrl: null,
			ownerId: OtherUser, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		DomainSeed.AddMember(inGuildA, userId: DeletedUser, joinedAt: Now);
		var inGuildB = GuildEntity.Create(
			id: 200, name: "B", description: null, iconUrl: null, bannerUrl: null,
			ownerId: OtherUser, everyoneRoleId: 201, adminRoleId: 202, now: Now).Value;
		DomainSeed.AddMember(inGuildB, userId: DeletedUser, joinedAt: Now);
		var notAMember = GuildEntity.Create(
			id: 300, name: "C", description: null, iconUrl: null, bannerUrl: null,
			ownerId: OtherUser, everyoneRoleId: 301, adminRoleId: 302, now: Now).Value;
		guilds.Add(inGuildA);
		guilds.Add(inGuildB);
		guilds.Add(notAMember);

		var bus = new FakeEventBus();
		var handler = HandlerFactory.CreateCommand<PurgeUserDataCommand>(
			guilds, new FakeGuildBanRepository(), new FakeGuildInviteRepository(),
			new FakeChannelPermissionOverwriteRepository(), bus);

		await handler.HandleAsync(new PurgeUserDataCommand(DeletedUser));

		var left = bus.Published.OfType<GuildMemberLeft>().ToList();
		Assert.Equal(2, left.Count);
		Assert.All(left, e => Assert.Equal(DeletedUser, e.UserId));
		Assert.Contains(left, e => e.GuildId == 100);
		Assert.Contains(left, e => e.GuildId == 200);
		Assert.DoesNotContain(left, e => e.GuildId == 300);
	}
}
