using Guild.Application.Features.Guilds.ListMyGuilds;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class ListMyGuildsHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task NoMemberships_ReturnsEmptyList()
	{
		var repo = new FakeGuildRepository();
		Seed(repo, id: 100, ownerId: 1);
		var handler = HandlerFactory.CreateQuery<ListMyGuildsQuery, Result<MyGuildListResponse>>(
			repo, new FakeCurrentUser { Id = 42 });

		var result = await handler.HandleAsync(new ListMyGuildsQuery());

		Assert.True(result.Succeeded);
		Assert.Empty(result.Value.Items);
	}

	[Fact]
	public async Task Member_ReturnsOnlyGuildsTheyBelongTo()
	{
		var repo = new FakeGuildRepository();
		Seed(repo, id: 100, ownerId: 1);
		Seed(repo, id: 200, ownerId: 1);
		Seed(repo, id: 300, ownerId: 2); // caller is not a member here

		var handler = HandlerFactory.CreateQuery<ListMyGuildsQuery, Result<MyGuildListResponse>>(
			repo, new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(new ListMyGuildsQuery());

		Assert.True(result.Succeeded);
		Assert.Equal(2, result.Value.Items.Count);
		Assert.All(result.Value.Items, g => Assert.NotEqual("300", g.Id));
	}

	[Fact]
	public async Task Entry_CarriesSummaryShape()
	{
		var repo = new FakeGuildRepository();
		Seed(repo, id: 100, ownerId: 1);
		var handler = HandlerFactory.CreateQuery<ListMyGuildsQuery, Result<MyGuildListResponse>>(
			repo, new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(new ListMyGuildsQuery());

		Assert.True(result.Succeeded);
		var entry = Assert.Single(result.Value.Items);
		Assert.Equal("100", entry.Id);
		Assert.Equal("1", entry.OwnerId);
		Assert.Equal(1, entry.MemberCount);
		Assert.Equal(Now, entry.JoinedAt);
	}

	private static void Seed(FakeGuildRepository repo, long id, long ownerId)
	{
		var guild = GuildEntity.Create(
			id: id, name: $"Guild {id}", description: null, iconUrl: null, bannerUrl: null,
			ownerId: ownerId, everyoneRoleId: id + 1, adminRoleId: id + 2, now: Now).Value;
		repo.Add(guild);
	}
}
