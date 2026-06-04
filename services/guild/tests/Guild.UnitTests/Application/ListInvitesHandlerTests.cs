using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Invites.Common;
using Guild.Application.Features.Invites.ListInvites;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class ListInvitesHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_Returns404()
	{
		var (g, i) = (new FakeGuildRepository(), new FakeGuildInviteRepository());
		var handler = NewHandler(g, i, callerId: 1);

		var result = await handler.HandleAsync(new ListInvitesQuery(GuildId: 9999));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task NonMember_ReturnsNotAMember()
	{
		var (g, i) = (new FakeGuildRepository(), new FakeGuildInviteRepository());
		var guild = SeedGuild(g, ownerId: 1);
		var handler = NewHandler(g, i, callerId: 99);

		var result = await handler.HandleAsync(new ListInvitesQuery(guild.Id));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
	}

	[Fact]
	public async Task MemberWithoutManageGuild_ReturnsMissingPermission()
	{
		var (g, i) = (new FakeGuildRepository(), new FakeGuildInviteRepository());
		var guild = SeedGuild(g, ownerId: 1);
		DomainSeed.AddMember(guild, userId: 99, joinedAt: Now);
		var handler = NewHandler(g, i, callerId: 99);

		var result = await handler.HandleAsync(new ListInvitesQuery(guild.Id));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.MissingPermission", result.Error.Code);
	}

	[Fact]
	public async Task HappyPath_ReturnsActiveInvites_NewestFirst()
	{
		var (g, i) = (new FakeGuildRepository(), new FakeGuildInviteRepository());
		var guild = SeedGuild(g, ownerId: 1);

		i.Seed(GuildInvite.Create("old", guild.Id, 1, null, null, Now).Value);
		i.Seed(GuildInvite.Create("new", guild.Id, 1, null, null, Now.AddMinutes(1)).Value);
		var revoked = GuildInvite.Create("revoked", guild.Id, 1, null, null, Now.AddSeconds(30)).Value;
		revoked.Revoke();
		i.Seed(revoked);

		var handler = NewHandler(g, i, callerId: 1);
		var result = await handler.HandleAsync(new ListInvitesQuery(guild.Id));

		Assert.True(result.Succeeded);
		Assert.Equal(2, result.Value.Count);
		Assert.Equal("new", result.Value[0].Code);
		Assert.Equal("old", result.Value[1].Code);
	}

	[Fact]
	public async Task EmptyList_ReturnsEmpty()
	{
		var (g, i) = (new FakeGuildRepository(), new FakeGuildInviteRepository());
		var guild = SeedGuild(g, ownerId: 1);
		var handler = NewHandler(g, i, callerId: 1);

		var result = await handler.HandleAsync(new ListInvitesQuery(guild.Id));

		Assert.True(result.Succeeded);
		Assert.Empty(result.Value);
	}

	private static IQueryHandler<ListInvitesQuery, Result<IReadOnlyList<InviteDto>>> NewHandler(
		FakeGuildRepository guilds, FakeGuildInviteRepository invites, long callerId)
	{
		return HandlerFactory.CreateQuery<ListInvitesQuery, Result<IReadOnlyList<InviteDto>>>(
			guilds, invites, new FakeCurrentUser { Id = callerId });
	}

	private static GuildEntity SeedGuild(FakeGuildRepository repo, long ownerId)
	{
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: ownerId, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		repo.AddAsync(guild).GetAwaiter().GetResult();
		return guild;
	}
}
