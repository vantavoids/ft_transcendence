using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Bans.Common;
using Guild.Application.Features.Bans.ListBans;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class ListBansHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_Returns_GuildNotFound()
	{
		var (handler, _, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new ListBansQuery(999, After: null, Limit: 50));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task CallerNotMember_Returns_NotAMember()
	{
		var (handler, _, _) = MakeHandler(currentUser: 99);

		var result = await handler.HandleAsync(new ListBansQuery(100, After: null, Limit: 50));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
	}

	[Fact]
	public async Task CallerWithoutBanMembers_Returns_MissingPermission()
	{
		var (handler, guilds, _) = MakeHandler(currentUser: 2);
		DomainSeed.AddMember(guilds.Store[100], userId: 2, joinedAt: Now);

		var result = await handler.HandleAsync(new ListBansQuery(100, After: null, Limit: 50));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.MissingPermission", result.Error.Code);
	}

	[Fact]
	public async Task HappyPath_NoBans_Returns_EmptyList()
	{
		var (handler, _, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new ListBansQuery(100, After: null, Limit: 50));

		Assert.True(result.Succeeded);
		Assert.Empty(result.Value.Items);
	}

	[Fact]
	public async Task HappyPath_ReturnsBans_OrderedByUserId_AndMapped()
	{
		var (handler, _, bans) = MakeHandler(currentUser: 1);
		bans.Seed(GuildBan.Create(guildId: 100, userId: 5, bannedBy: 1, reason: "b", now: Now).Value);
		bans.Seed(GuildBan.Create(guildId: 100, userId: 3, bannedBy: 1, reason: "a", now: Now).Value);

		var result = await handler.HandleAsync(new ListBansQuery(100, After: null, Limit: 50));

		Assert.True(result.Succeeded);
		Assert.Collection(result.Value.Items,
			b => Assert.Equal("3", b.UserId),
			b => Assert.Equal("5", b.UserId));
		Assert.Equal("1", result.Value.Items[0].BannedBy);
		Assert.Equal("a", result.Value.Items[0].Reason);
	}

	[Fact]
	public async Task HappyPath_RespectsAfterAndLimit()
	{
		var (handler, _, bans) = MakeHandler(currentUser: 1);
		bans.Seed(GuildBan.Create(guildId: 100, userId: 3, bannedBy: 1, reason: null, now: Now).Value);
		bans.Seed(GuildBan.Create(guildId: 100, userId: 5, bannedBy: 1, reason: null, now: Now).Value);
		bans.Seed(GuildBan.Create(guildId: 100, userId: 7, bannedBy: 1, reason: null, now: Now).Value);

		var result = await handler.HandleAsync(new ListBansQuery(100, After: 3, Limit: 1));

		Assert.True(result.Succeeded);
		var only = Assert.Single(result.Value.Items);
		Assert.Equal("5", only.UserId);
	}

	private static (
		IQueryHandler<ListBansQuery, Result<BanListResponse>> Handler,
		FakeGuildRepository Guilds,
		FakeGuildBanRepository Bans)
		MakeHandler(long currentUser)
	{
		var guilds = new FakeGuildRepository();
		var bans = new FakeGuildBanRepository();
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		guilds.AddAsync(guild).GetAwaiter().GetResult();

		var handler = HandlerFactory.CreateQuery<ListBansQuery, Result<BanListResponse>>(
			guilds, bans, new FakeCurrentUser { Id = currentUser });
		return (handler, guilds, bans);
	}
}
