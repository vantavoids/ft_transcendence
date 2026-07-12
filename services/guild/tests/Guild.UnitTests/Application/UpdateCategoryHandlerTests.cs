using System.Reflection;
using Guild.Application.Features.Categories.Common;
using Guild.Application.Features.Categories.UpdateCategory;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class UpdateCategoryHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_ReturnsGuildNotFound()
	{
		var guildRepo = new FakeGuildRepository();
		var catRepo = new FakeChannelCategoryRepository();
		var handler = HandlerFactory.CreateCommand<UpdateCategoryCommand, Result<CategoryResponse>>(
			guildRepo, catRepo, new FakeEventBus(),new FakeClock(), new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(
			new UpdateCategoryCommand(GuildId: 999, CategoryId: 1, Name: "x", Position: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task NonMember_ReturnsNotAMember()
	{
		var guildRepo = new FakeGuildRepository();
		var catRepo = new FakeChannelCategoryRepository();
		Seed(guildRepo, catRepo, ownerId: 1);

		var handler = HandlerFactory.CreateCommand<UpdateCategoryCommand, Result<CategoryResponse>>(
			guildRepo, catRepo, new FakeEventBus(),new FakeClock(), new FakeCurrentUser { Id = 99 });

		var result = await handler.HandleAsync(
			new UpdateCategoryCommand(GuildId: 100, CategoryId: 500, Name: "x", Position: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
	}

	[Fact]
	public async Task MemberWithoutManageChannels_ReturnsMissingPermission()
	{
		var guildRepo = new FakeGuildRepository();
		var catRepo = new FakeChannelCategoryRepository();
		var guild = CreateGuildWithBareMember(memberId: 2, ownerId: 1);
		guildRepo.Add(guild);
		SeedCategory(catRepo, id: 500);

		var handler = HandlerFactory.CreateCommand<UpdateCategoryCommand, Result<CategoryResponse>>(
			guildRepo, catRepo, new FakeEventBus(),new FakeClock(), new FakeCurrentUser { Id = 2 });

		var result = await handler.HandleAsync(
			new UpdateCategoryCommand(GuildId: 100, CategoryId: 500, Name: "x", Position: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.MissingPermission", result.Error.Code);
	}

	[Fact]
	public async Task UnknownCategory_ReturnsCategoryNotFound()
	{
		var guildRepo = new FakeGuildRepository();
		var catRepo = new FakeChannelCategoryRepository();
		Seed(guildRepo, catRepo, ownerId: 1, seedCategory: false);

		var handler = HandlerFactory.CreateCommand<UpdateCategoryCommand, Result<CategoryResponse>>(
			guildRepo, catRepo, new FakeEventBus(),new FakeClock(), new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(
			new UpdateCategoryCommand(GuildId: 100, CategoryId: 9999, Name: "x", Position: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.CategoryNotFound", result.Error.Code);
	}

	[Fact]
	public async Task InvalidName_ReturnsFailure()
	{
		var guildRepo = new FakeGuildRepository();
		var catRepo = new FakeChannelCategoryRepository();
		Seed(guildRepo, catRepo, ownerId: 1);

		var handler = HandlerFactory.CreateCommand<UpdateCategoryCommand, Result<CategoryResponse>>(
			guildRepo, catRepo, new FakeEventBus(),new FakeClock(), new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(
			new UpdateCategoryCommand(GuildId: 100, CategoryId: 500, Name: "", Position: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.CategoryNameRequired", result.Error.Code);
	}

	[Fact]
	public async Task RenameOnly_UpdatesNameNotPosition()
	{
		var guildRepo = new FakeGuildRepository();
		var catRepo = new FakeChannelCategoryRepository();
		Seed(guildRepo, catRepo, ownerId: 1);
		var original = catRepo.Store[500];
		var originalPosition = original.Position;

		var handler = HandlerFactory.CreateCommand<UpdateCategoryCommand, Result<CategoryResponse>>(
			guildRepo, catRepo, new FakeEventBus(),new FakeClock(), new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(
			new UpdateCategoryCommand(GuildId: 100, CategoryId: 500, Name: "Renamed", Position: null));

		Assert.True(result.Succeeded);
		Assert.Equal("Renamed", result.Value.Name);
		Assert.Equal(originalPosition, result.Value.Position);
	}

	[Fact]
	public async Task RepositionOnly_UpdatesPositionNotName()
	{
		var guildRepo = new FakeGuildRepository();
		var catRepo = new FakeChannelCategoryRepository();
		Seed(guildRepo, catRepo, ownerId: 1);
		var originalName = catRepo.Store[500].Name;

		var handler = HandlerFactory.CreateCommand<UpdateCategoryCommand, Result<CategoryResponse>>(
			guildRepo, catRepo, new FakeEventBus(),new FakeClock(), new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(
			new UpdateCategoryCommand(GuildId: 100, CategoryId: 500, Name: null, Position: 9));

		Assert.True(result.Succeeded);
		Assert.Equal(originalName, result.Value.Name);
		Assert.Equal(9, result.Value.Position);
	}

	[Fact]
	public async Task BothFields_UpdatesBoth()
	{
		var guildRepo = new FakeGuildRepository();
		var catRepo = new FakeChannelCategoryRepository();
		Seed(guildRepo, catRepo, ownerId: 1);

		var handler = HandlerFactory.CreateCommand<UpdateCategoryCommand, Result<CategoryResponse>>(
			guildRepo, catRepo, new FakeEventBus(),new FakeClock(), new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(
			new UpdateCategoryCommand(GuildId: 100, CategoryId: 500, Name: "New", Position: 12));

		Assert.True(result.Succeeded);
		Assert.Equal("New", result.Value.Name);
		Assert.Equal(12, result.Value.Position);
		Assert.Equal(1, catRepo.UpdateCount);
	}

	[Fact]
	public async Task BothNull_NoChange_StillReturnsCategory()
	{
		var guildRepo = new FakeGuildRepository();
		var catRepo = new FakeChannelCategoryRepository();
		Seed(guildRepo, catRepo, ownerId: 1);
		var originalName = catRepo.Store[500].Name;
		var originalPosition = catRepo.Store[500].Position;

		var handler = HandlerFactory.CreateCommand<UpdateCategoryCommand, Result<CategoryResponse>>(
			guildRepo, catRepo, new FakeEventBus(),new FakeClock(), new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(
			new UpdateCategoryCommand(GuildId: 100, CategoryId: 500, Name: null, Position: null));

		Assert.True(result.Succeeded);
		Assert.Equal(originalName, result.Value.Name);
		Assert.Equal(originalPosition, result.Value.Position);
	}

	[Fact]
	public async Task MemberCheck_UsesAnyNotAll_InMultiMemberGuild()
	{
		// Any(m => m.UserId == cuid) succeeds when cuid is one of several members;
		// All would fail because the other member has a different userId
		var guildRepo = new FakeGuildRepository();
		var catRepo = new FakeChannelCategoryRepository();
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		guildRepo.Add(guild);
		SeedCategory(catRepo, id: 500);

		var handler = HandlerFactory.CreateCommand<UpdateCategoryCommand, Result<CategoryResponse>>(
			guildRepo, catRepo, new FakeEventBus(),new FakeClock(), new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(
			new UpdateCategoryCommand(GuildId: 100, CategoryId: 500, Name: "Renamed", Position: null));

		Assert.True(result.Succeeded);
	}

	private static void Seed(
		FakeGuildRepository guildRepo,
		FakeChannelCategoryRepository catRepo,
		long ownerId,
		bool seedCategory = true)
	{
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: ownerId, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		guildRepo.Add(guild);
		if (seedCategory)
			SeedCategory(catRepo, id: 500);
	}

	private static void SeedCategory(FakeChannelCategoryRepository repo, long id)
	{
		repo.Seed(ChannelCategory.Create(
			id: id, guildId: 100, name: "Initial", position: 0, now: Now).Value);
	}

	private static GuildEntity CreateGuildWithBareMember(long memberId, long ownerId)
	{
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: memberId, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		DomainSeed.SetOwnerId(guild, newOwnerId: ownerId);
		var memberRolesField = typeof(GuildEntity).GetField("_memberRoles",
			BindingFlags.Instance | BindingFlags.NonPublic)!;
		var list = (List<MemberRole>)memberRolesField.GetValue(guild)!;
		list.Clear();
		return guild;
	}
}
