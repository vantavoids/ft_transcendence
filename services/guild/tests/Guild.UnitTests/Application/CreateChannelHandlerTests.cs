using System.Reflection;
using Guild.Application.Features.Channels.Common;
using Guild.Application.Features.Channels.CreateChannel;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class CreateChannelHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_ReturnsGuildNotFound()
	{
		var handler = MakeHandler(out _, out var channels, out _, ownerSeed: null, currentUser: 1);

		var result = await handler.HandleAsync(
			new CreateChannelCommand(GuildId: 999, Name: "general", Type: "text",
				CategoryId: null, Topic: null, Position: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
		Assert.Empty(channels.Store);
	}

	[Fact]
	public async Task NonMember_ReturnsNotAMember()
	{
		var handler = MakeHandler(out _, out _, out _, ownerSeed: 1, currentUser: 99);

		var result = await handler.HandleAsync(
			new CreateChannelCommand(GuildId: 100, Name: "general", Type: "text",
				CategoryId: null, Topic: null, Position: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
	}

	[Fact]
	public async Task MemberWithoutManageChannels_ReturnsMissingPermission()
	{
		var guildRepo = new FakeGuildRepository();
		var channelRepo = new FakeChannelRepository();
		var categoryRepo = new FakeChannelCategoryRepository();
		var guild = CreateGuildWithBareMember(memberId: 2, ownerId: 1);
		await guildRepo.AddAsync(guild);

		var handler = HandlerFactory.CreateCommand<CreateChannelCommand, Result<ChannelResponse>>(
			guildRepo, channelRepo, categoryRepo,
			new FakeIdGenerator(), new FakeClock(), new FakeCurrentUser { Id = 2 });

		var result = await handler.HandleAsync(
			new CreateChannelCommand(GuildId: 100, Name: "general", Type: "text",
				CategoryId: null, Topic: null, Position: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.MissingPermission", result.Error.Code);
	}

	[Fact]
	public async Task UnknownCategory_ReturnsCategoryNotFound()
	{
		var handler = MakeHandler(out _, out _, out _, ownerSeed: 1, currentUser: 1);

		var result = await handler.HandleAsync(
			new CreateChannelCommand(GuildId: 100, Name: "general", Type: "text",
				CategoryId: 5555, Topic: null, Position: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.CategoryNotFound", result.Error.Code);
	}

	[Fact]
	public async Task InvalidType_ReturnsChannelInvalidType()
	{
		var handler = MakeHandler(out _, out _, out _, ownerSeed: 1, currentUser: 1);

		var result = await handler.HandleAsync(
			new CreateChannelCommand(GuildId: 100, Name: "general", Type: "weird",
				CategoryId: null, Topic: null, Position: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.ChannelInvalidType", result.Error.Code);
	}

	[Fact]
	public async Task HappyPath_DefaultType_IsText()
	{
		var handler = MakeHandler(out _, out var channels, out _, ownerSeed: 1, currentUser: 1);

		var result = await handler.HandleAsync(
			new CreateChannelCommand(GuildId: 100, Name: "general", Type: null,
				CategoryId: null, Topic: null, Position: 0));

		Assert.True(result.Succeeded);
		Assert.Equal("text", result.Value.Type);
		Assert.Single(channels.Store);
	}

	[Fact]
	public async Task HappyPath_AutoAppendsPosition_PerCategory()
	{
		var handler = MakeHandler(out _, out var channels, out _, ownerSeed: 1, currentUser: 1);

		// pre-seed two channels in null category with positions 0 and 3
		channels.Seed(Channel.Create(900, 100, null, "a", null, ChannelType.Text, 0, Now).Value);
		channels.Seed(Channel.Create(901, 100, null, "b", null, ChannelType.Text, 3, Now).Value);
		// one in category 50 with position 9 - should NOT influence the next null-category append
		channels.Seed(Channel.Create(902, 100, 50, "c", null, ChannelType.Text, 9, Now).Value);

		var result = await handler.HandleAsync(
			new CreateChannelCommand(GuildId: 100, Name: "next", Type: "text",
				CategoryId: null, Topic: null, Position: null));

		Assert.True(result.Succeeded);
		Assert.Equal(4, result.Value.Position);
	}

	private static Guild.Application.Abstractions.Messaging.ICommandHandler<CreateChannelCommand, Result<ChannelResponse>>
		MakeHandler(
			out FakeGuildRepository guilds,
			out FakeChannelRepository channels,
			out FakeChannelCategoryRepository categories,
			long? ownerSeed,
			long currentUser)
	{
		guilds = new FakeGuildRepository();
		channels = new FakeChannelRepository();
		categories = new FakeChannelCategoryRepository();

		if (ownerSeed is long ownerId)
		{
			var guild = GuildEntity.Create(
				id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
				ownerId: ownerId, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
			guilds.AddAsync(guild).GetAwaiter().GetResult();
		}

		return HandlerFactory.CreateCommand<CreateChannelCommand, Result<ChannelResponse>>(
			guilds, channels, categories,
			new FakeIdGenerator(), new FakeClock(), new FakeCurrentUser { Id = currentUser });
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
