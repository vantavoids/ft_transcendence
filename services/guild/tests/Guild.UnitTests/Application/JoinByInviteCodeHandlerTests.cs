using Guild.Application.Abstractions.Messaging;
using Guild.Application.Contracts;
using Guild.Application.Features.Guilds.Common;
using Guild.Application.Features.Membership.JoinByInviteCode;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class JoinByInviteCodeHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task EmptyCode_ReturnsInviteCodeRequired()
	{
		var (g, i, bans, b, u) = NewFakes();
		var handler = NewHandler(g, i, bans, b, u, callerId: 99);

		var result = await handler.HandleAsync(new JoinByInviteCodeCommand(Code: null, ExpectedGuildId: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.InviteCodeRequired", result.Error.Code);
	}

	[Fact]
	public async Task UnknownCode_ReturnsInviteNotFound()
	{
		var (g, i, bans, b, u) = NewFakes();
		var handler = NewHandler(g, i, bans, b, u, callerId: 99);

		var result = await handler.HandleAsync(new JoinByInviteCodeCommand(Code: "ghost", ExpectedGuildId: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.InviteNotFound", result.Error.Code);
	}

	[Fact]
	public async Task ExpectedGuildMismatch_ReturnsInviteGuildMismatch()
	{
		var (g, i, bans, b, u) = NewFakes();
		var guild = SeedGuild(g, ownerId: 1);
		var invite = SeedInvite(i, guildId: guild.Id, creator: 1);
		var handler = NewHandler(g, i, bans, b, u, callerId: 99);

		var result = await handler.HandleAsync(
			new JoinByInviteCodeCommand(invite.Code, ExpectedGuildId: guild.Id + 1));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.InviteGuildMismatch", result.Error.Code);
	}

	[Fact]
	public async Task GuildMissing_ReturnsGuildNotFound()
	{
		var (g, i, bans, b, u) = NewFakes();
		// invite exists pointing at a guild that does not (data drift / cascade lag)
		var invite = SeedInvite(i, guildId: 12345, creator: 1);
		var handler = NewHandler(g, i, bans, b, u, callerId: 99);

		var result = await handler.HandleAsync(new JoinByInviteCodeCommand(invite.Code, ExpectedGuildId: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task AlreadyMember_ReturnsAlreadyMember()
	{
		var (g, i, bans, b, u) = NewFakes();
		var guild = SeedGuild(g, ownerId: 1);
		var invite = SeedInvite(i, guildId: guild.Id, creator: 1);
		var handler = NewHandler(g, i, bans, b, u, callerId: 1);

		var result = await handler.HandleAsync(new JoinByInviteCodeCommand(invite.Code, ExpectedGuildId: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.AlreadyMember", result.Error.Code);
	}

	[Fact]
	public async Task RevokedInvite_ReturnsInviteUnusable()
	{
		var (g, i, bans, b, u) = NewFakes();
		var guild = SeedGuild(g, ownerId: 1);
		var invite = SeedInvite(i, guildId: guild.Id, creator: 1);
		invite.Revoke();
		var handler = NewHandler(g, i, bans, b, u, callerId: 99);

		var result = await handler.HandleAsync(new JoinByInviteCodeCommand(invite.Code, ExpectedGuildId: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.InviteUnusable", result.Error.Code);
	}

	[Fact]
	public async Task ExpiredInvite_ReturnsInviteUnusable_DoesNotLoadGuild()
	{
		var (g, i, bans, b, u) = NewFakes();
		var invite = GuildInvite.Create("abc123", guildId: 100, createdBy: 1,
			maxUses: null, expiresAt: Now.AddHours(1), now: Now).Value;
		i.Seed(invite);
		// guild is intentionally not seeded; the fail-fast must reject the invite
		// without ever reaching the guild repository
		var handler = HandlerFactory.CreateCommand<JoinByInviteCodeCommand, Result<GuildDto>>(
			g, i, bans, b, u, new FakeClock(Now.AddHours(2)), new FakeCurrentUser { Id = 99 });

		var result = await handler.HandleAsync(new JoinByInviteCodeCommand(invite.Code, ExpectedGuildId: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.InviteUnusable", result.Error.Code);
	}

	[Fact]
	public async Task ExhaustedInvite_ReturnsInviteUnusable()
	{
		var (g, i, bans, b, u) = NewFakes();
		var guild = SeedGuild(g, ownerId: 1);
		var invite = SeedInvite(i, guildId: guild.Id, creator: 1, maxUses: 1);
		invite.Consume(Now);
		var handler = NewHandler(g, i, bans, b, u, callerId: 99);

		var result = await handler.HandleAsync(new JoinByInviteCodeCommand(invite.Code, ExpectedGuildId: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.InviteUnusable", result.Error.Code);
	}

	[Fact]
	public async Task HappyPath_AddsMember_ConsumesInvite_PublishesEvent_HitsUserService()
	{
		var (g, i, bans, b, u) = NewFakes();
		var guild = SeedGuild(g, ownerId: 1);
		var invite = SeedInvite(i, guildId: guild.Id, creator: 1, maxUses: 3);
		var handler = NewHandler(g, i, bans, b, u, callerId: 99);

		var result = await handler.HandleAsync(new JoinByInviteCodeCommand(invite.Code, ExpectedGuildId: guild.Id));

		Assert.True(result.Succeeded);
		Assert.Equal(guild.Id.ToString(), result.Value.Id);
		Assert.Contains(guild.Members, m => m.UserId == 99);
		Assert.Equal(1, invite.Uses);
		Assert.Equal(1, i.UpdateCount);
		Assert.Equal(1, u.ExistsCallCount);

		var evt = b.Single<GuildMemberJoined>();
		Assert.Equal(guild.Id, evt.GuildId);
		Assert.Equal("Test", evt.GuildName);
		Assert.Equal(99L, evt.UserId);
	}

	[Fact]
	public async Task UserServiceUnavailable_StillJoins()
	{
		var (g, i, bans, b, u) = NewFakes();
		u.Exists = false;
		var guild = SeedGuild(g, ownerId: 1);
		var invite = SeedInvite(i, guildId: guild.Id, creator: 1);
		var handler = NewHandler(g, i, bans, b, u, callerId: 99);

		var result = await handler.HandleAsync(new JoinByInviteCodeCommand(invite.Code, ExpectedGuildId: null));

		Assert.True(result.Succeeded);
		Assert.Contains(guild.Members, m => m.UserId == 99);
		Assert.True(b.Has<GuildMemberJoined>());
	}

	private static (FakeGuildRepository, FakeGuildInviteRepository, FakeGuildBanRepository, FakeEventBus, FakeUserService) NewFakes()
		=> (new FakeGuildRepository(), new FakeGuildInviteRepository(), new FakeGuildBanRepository(), new FakeEventBus(), new FakeUserService());

	private static ICommandHandler<JoinByInviteCodeCommand, Result<GuildDto>> NewHandler(
		FakeGuildRepository guilds,
		FakeGuildInviteRepository invites,
		FakeGuildBanRepository bans,
		FakeEventBus bus,
		FakeUserService users,
		long callerId)
	{
		return HandlerFactory.CreateCommand<JoinByInviteCodeCommand, Result<GuildDto>>(
			guilds, invites, bans, bus, users, new FakeClock(), new FakeCurrentUser { Id = callerId });
	}

	private static GuildEntity SeedGuild(FakeGuildRepository repo, long ownerId)
	{
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: ownerId, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		repo.AddAsync(guild).GetAwaiter().GetResult();
		return guild;
	}

	private static GuildInvite SeedInvite(
		FakeGuildInviteRepository repo, long guildId, long creator, int? maxUses = null)
	{
		var invite = GuildInvite.Create(
			code: "abc123", guildId: guildId, createdBy: creator,
			maxUses: maxUses, expiresAt: null, now: Now).Value;
		repo.Seed(invite);
		return invite;
	}
}
