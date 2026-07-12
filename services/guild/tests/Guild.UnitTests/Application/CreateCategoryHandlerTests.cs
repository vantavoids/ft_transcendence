using System.Reflection;
using Guild.Application.Features.Categories.Common;
using Guild.Application.Features.Categories.CreateCategory;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class CreateCategoryHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_ReturnsGuildNotFound()
	{
		var guildRepo = new FakeGuildRepository();
		var catRepo = new FakeChannelCategoryRepository();
		var handler = HandlerFactory.CreateCommand<CreateCategoryCommand, Result<CategoryResponse>>(
			guildRepo, catRepo, new FakeEventBus(),new FakeIdGenerator(), new FakeClock(), new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(
			new CreateCategoryCommand(GuildId: 999, Name: "Text", Position: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task NonMember_ReturnsNotAMember()
	{
		var guildRepo = new FakeGuildRepository();
		Seed(guildRepo, ownerId: 1);
		var catRepo = new FakeChannelCategoryRepository();
		var handler = HandlerFactory.CreateCommand<CreateCategoryCommand, Result<CategoryResponse>>(
			guildRepo, catRepo, new FakeEventBus(),new FakeIdGenerator(), new FakeClock(), new FakeCurrentUser { Id = 99 });

		var result = await handler.HandleAsync(
			new CreateCategoryCommand(GuildId: 100, Name: "Text", Position: null));

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

		var handler = HandlerFactory.CreateCommand<CreateCategoryCommand, Result<CategoryResponse>>(
			guildRepo, catRepo, new FakeEventBus(),new FakeIdGenerator(), new FakeClock(), new FakeCurrentUser { Id = 2 });

		var result = await handler.HandleAsync(
			new CreateCategoryCommand(GuildId: 100, Name: "Text", Position: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.MissingPermission", result.Error.Code);
	}

	[Fact]
	public async Task InvalidName_ReturnsFailure_NoPersist()
	{
		var guildRepo = new FakeGuildRepository();
		var catRepo = new FakeChannelCategoryRepository();
		Seed(guildRepo, ownerId: 1);

		var handler = HandlerFactory.CreateCommand<CreateCategoryCommand, Result<CategoryResponse>>(
			guildRepo, catRepo, new FakeEventBus(),new FakeIdGenerator(), new FakeClock(), new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(
			new CreateCategoryCommand(GuildId: 100, Name: "", Position: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.CategoryNameRequired", result.Error.Code);
		Assert.Empty(catRepo.Store);
	}

	[Fact]
	public async Task HappyPath_WithExplicitPosition_PersistsAndReturnsDto()
	{
		var guildRepo = new FakeGuildRepository();
		var catRepo = new FakeChannelCategoryRepository();
		Seed(guildRepo, ownerId: 1);

		var handler = HandlerFactory.CreateCommand<CreateCategoryCommand, Result<CategoryResponse>>(
			guildRepo, catRepo, new FakeEventBus(),new FakeIdGenerator(), new FakeClock(), new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(
			new CreateCategoryCommand(GuildId: 100, Name: "Text", Position: 5));

		Assert.True(result.Succeeded);
		Assert.Equal("Text", result.Value.Name);
		Assert.Equal(5, result.Value.Position);
		Assert.Equal("100", result.Value.GuildId);
		Assert.True(long.TryParse(result.Value.Id, out _));
		Assert.Single(catRepo.Store);
	}

	[Fact]
	public async Task HappyPath_WithoutPosition_AutoAppendsToZero_WhenNoCategories()
	{
		var guildRepo = new FakeGuildRepository();
		var catRepo = new FakeChannelCategoryRepository();
		Seed(guildRepo, ownerId: 1);

		var handler = HandlerFactory.CreateCommand<CreateCategoryCommand, Result<CategoryResponse>>(
			guildRepo, catRepo, new FakeEventBus(),new FakeIdGenerator(), new FakeClock(), new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(
			new CreateCategoryCommand(GuildId: 100, Name: "First", Position: null));

		Assert.True(result.Succeeded);
		Assert.Equal(0, result.Value.Position);
	}

	[Fact]
	public async Task HappyPath_WithoutPosition_ComputesMaxPlusOne()
	{
		var guildRepo = new FakeGuildRepository();
		var catRepo = new FakeChannelCategoryRepository();
		Seed(guildRepo, ownerId: 1);

		// seed two existing categories with positions 2 and 5
		catRepo.Seed(ChannelCategory.Create(id: 9001, guildId: 100, name: "A", position: 2, now: Now).Value);
		catRepo.Seed(ChannelCategory.Create(id: 9002, guildId: 100, name: "B", position: 5, now: Now).Value);

		var handler = HandlerFactory.CreateCommand<CreateCategoryCommand, Result<CategoryResponse>>(
			guildRepo, catRepo, new FakeEventBus(),new FakeIdGenerator(), new FakeClock(), new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(
			new CreateCategoryCommand(GuildId: 100, Name: "C", Position: null));

		Assert.True(result.Succeeded);
		Assert.Equal(6, result.Value.Position);
	}

	[Fact]
	public async Task MemberCheck_UsesAnyNotAll_InMultiMemberGuild()
	{
		// Any(m => m.UserId == cuid) is true when cuid is ONE of several members.
		// All(m => m.UserId == cuid) would be false (the other member has a different
		// userId), incorrectly rejecting a valid member as NotAMember.
		var guildRepo = new FakeGuildRepository();
		var catRepo = new FakeChannelCategoryRepository();
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		guildRepo.Add(guild);

		var handler = HandlerFactory.CreateCommand<CreateCategoryCommand, Result<CategoryResponse>>(
			guildRepo, catRepo, new FakeEventBus(),new FakeIdGenerator(), new FakeClock(), new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(
			new CreateCategoryCommand(GuildId: 100, Name: "General", Position: 0));

		Assert.True(result.Succeeded);
	}

	private static void Seed(FakeGuildRepository repo, long ownerId)
	{
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: ownerId, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		repo.Add(guild);
	}

	private static GuildEntity CreateGuildWithBareMember(long memberId, long ownerId)
	{
		// build the guild so the entity factory wires the admin role to memberId,
		// then swap owner to a different user and clear MemberRoles so the original
		// member has nothing but @everyone (no ManageChannels)
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
