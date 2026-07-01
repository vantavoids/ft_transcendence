using Guild.Application.Features.Categories.Common;
using Guild.Application.Features.Categories.ListCategories;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class ListCategoriesHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_ReturnsGuildNotFound()
	{
		var guildRepo = new FakeGuildRepository();
		var categoryRepo = new FakeChannelCategoryRepository();
		var handler = HandlerFactory.CreateQuery<ListCategoriesQuery, Result<CategoryListResponse>>(
			guildRepo, categoryRepo, new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(new ListCategoriesQuery(GuildId: 999));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task NonMember_ReturnsNotAMember()
	{
		var guildRepo = new FakeGuildRepository();
		Seed(guildRepo, ownerId: 1);
		var categoryRepo = new FakeChannelCategoryRepository();
		var handler = HandlerFactory.CreateQuery<ListCategoriesQuery, Result<CategoryListResponse>>(
			guildRepo, categoryRepo, new FakeCurrentUser { Id = 99 });

		var result = await handler.HandleAsync(new ListCategoriesQuery(GuildId: 100));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
	}

	[Fact]
	public async Task Member_ReturnsCategoriesOrderedByPosition()
	{
		var guildRepo = new FakeGuildRepository();
		Seed(guildRepo, ownerId: 1);
		var categoryRepo = new FakeChannelCategoryRepository();
		categoryRepo.Seed(ChannelCategory.Create(1, 100, "b", 2, Now).Value);
		categoryRepo.Seed(ChannelCategory.Create(2, 100, "a", 1, Now).Value);

		var handler = HandlerFactory.CreateQuery<ListCategoriesQuery, Result<CategoryListResponse>>(
			guildRepo, categoryRepo, new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(new ListCategoriesQuery(GuildId: 100));

		Assert.True(result.Succeeded);
		Assert.Equal(2, result.Value.Items.Count);
		Assert.Equal("a", result.Value.Items[0].Name);
		Assert.Equal("b", result.Value.Items[1].Name);
		Assert.Equal("100", result.Value.Items[0].GuildId);
	}

	private static void Seed(FakeGuildRepository repo, long ownerId)
	{
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: ownerId, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		repo.Add(guild);
	}
}
