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
		guildRepo.Add(guild);

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

	[Fact]
	public async Task AutoPosition_FirstChannelInCategory_IsZero()
	{
		// contract default: first channel in an empty bucket lands at position 0
		// (kills the (max ?? -1) + 1 mutation that would otherwise produce a non-zero start)
		var handler = MakeHandler(out _, out var channels, out _, ownerSeed: 1, currentUser: 1);

		var result = await handler.HandleAsync(
			new CreateChannelCommand(GuildId: 100, Name: "first", Type: "text",
				CategoryId: null, Topic: null, Position: null));

		Assert.True(result.Succeeded);
		Assert.Equal(0, result.Value.Position);
		Assert.Single(channels.Store);
	}

	[Fact]
	public async Task IsNsfwTrue_PersistsAndIsEchoedInResponse()
	{
		// contract: is_nsfw is part of the channel object and must round-trip
		var handler = MakeHandler(out _, out var channels, out _, ownerSeed: 1, currentUser: 1);

		var result = await handler.HandleAsync(
			new CreateChannelCommand(GuildId: 100, Name: "after-dark", Type: "text",
				CategoryId: null, Topic: null, Position: 0, IsNsfw: true));

		Assert.True(result.Succeeded);
		Assert.True(result.Value.IsNsfw);
		Assert.True(channels.Store.Values.Single().IsNsfw);
	}

	[Fact]
	public async Task SlowmodeSecondsValue_PersistsAndIsEchoedInResponse()
	{
		// contract: slowmode_seconds is part of the channel object and must round-trip
		var handler = MakeHandler(out _, out var channels, out _, ownerSeed: 1, currentUser: 1);

		var result = await handler.HandleAsync(
			new CreateChannelCommand(GuildId: 100, Name: "throttled", Type: "text",
				CategoryId: null, Topic: null, Position: 0, SlowmodeSeconds: 30));

		Assert.True(result.Succeeded);
		Assert.Equal(30, result.Value.SlowmodeSeconds);
		Assert.Equal(30, channels.Store.Values.Single().SlowmodeSeconds);
	}

	[Fact]
	public async Task AnnouncementType_ParsesAndPersists()
	{
		// contract: "announcement" is a valid channel type alongside "text"
		var handler = MakeHandler(out _, out _, out _, ownerSeed: 1, currentUser: 1);

		var result = await handler.HandleAsync(
			new CreateChannelCommand(GuildId: 100, Name: "news", Type: "announcement",
				CategoryId: null, Topic: null, Position: 0));

		Assert.True(result.Succeeded);
		Assert.Equal("announcement", result.Value.Type);
	}

	[Fact]
	public async Task MemberCheck_UsesAllNotAny_InMultiMemberGuild()
	{
		// All(m => m.UserId != cuid) = false when cuid IS a member -> correct, proceeds.
		// Any(m => m.UserId != cuid) = true because the *other* member satisfies != cuid,
		// which would incorrectly reject the current user as NotAMember.
		var guilds = new FakeGuildRepository();
		var channels = new FakeChannelRepository();
		var cats = new FakeChannelCategoryRepository();
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		guilds.Add(guild);

		var handler = HandlerFactory.CreateCommand<CreateChannelCommand, Result<ChannelResponse>>(
			guilds, channels, cats,
			new FakeIdGenerator(), new FakeClock(), new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(
			new CreateChannelCommand(GuildId: 100, Name: "general", Type: "text",
				CategoryId: null, Topic: null, Position: 0));

		Assert.True(result.Succeeded);
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
			guilds.Add(guild);
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
