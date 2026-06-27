using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.Messages.Common;
using Chat.Application.Features.Messages.GetChannelMessages;
using Chat.Domain.Messages;
using Chat.Domain.Results;
using Chat.UnitTests.Fakes;
using Xunit;

namespace Chat.UnitTests.Application;

public sealed class GetChannelMessagesHandlerTests
{
	private const long ReadMessagesPermission = 1L << 1;
	private const long AdministratorPermission = 1L << 8;

	private sealed record Harness(
		FakeCurrentUser CurrentUser,
		FakeGuildClient GuildClient,
		FakeMessageRepository Repository,
		FakeAttachmentRepository AttachmentRepository,
		FakeClock Clock);

	private static (Harness Harness, IQueryHandler<GetChannelMessagesQuery, Result<IReadOnlyList<MessageResponse>>> Handler)
		BuildHandler(long userId = 42)
	{
		var currentUser = new FakeCurrentUser { UserId = userId };
		var guildClient = new FakeGuildClient();
		var repository = new FakeMessageRepository();
		var attachmentRepository = new FakeAttachmentRepository();
		var clock = new FakeClock();

		var handler = HandlerFactory.CreateQuery<GetChannelMessagesQuery, Result<IReadOnlyList<MessageResponse>>>(
			currentUser, guildClient, repository, attachmentRepository, clock);

		return (new Harness(currentUser, guildClient, repository, attachmentRepository, clock), handler);
	}

	private static Message SeedMessage(FakeMessageRepository repo, long id, long channelId, DateTimeOffset createdAt, bool isDeleted = false)
	{
		var message = Message.Reconstitute(
			id: id, channelId: channelId, authorId: 99,
			content: "hello", replyToId: null, editedAt: null,
			isDeleted: isDeleted, createdAt: createdAt);
		repo.Seed(message);
		return message;
	}

	[Fact]
	public async Task ChannelNotFound_ReturnsChannelNotFound()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = null;

		var result = await handler.HandleAsync(new GetChannelMessagesQuery(ChannelId: 100, BeforeTime: null, Limit: 50));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.ChannelNotFound, result.Error);
	}

	[Fact]
	public async Task NotAMember_ReturnsNotAMember()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: false, GuildId: 5, Permissions: ReadMessagesPermission);

		var result = await handler.HandleAsync(new GetChannelMessagesQuery(ChannelId: 100, BeforeTime: null, Limit: 50));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.NotAMember, result.Error);
	}

	[Fact]
	public async Task MemberWithoutReadPermission_ReturnsMissingReadPermission()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: 0);

		var result = await handler.HandleAsync(new GetChannelMessagesQuery(ChannelId: 100, BeforeTime: null, Limit: 50));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.MissingReadPermission, result.Error);
	}

	[Fact]
	public async Task HappyPath_ReturnsMessages_ExcludesDeleted()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);

		var anchor = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
		SeedMessage(h.Repository, id: 1, channelId: 100, createdAt: anchor.AddMinutes(-10));
		SeedMessage(h.Repository, id: 2, channelId: 100, createdAt: anchor.AddMinutes(-5));
		SeedMessage(h.Repository, id: 3, channelId: 100, createdAt: anchor.AddMinutes(-2), isDeleted: true);

		var result = await handler.HandleAsync(new GetChannelMessagesQuery(ChannelId: 100, BeforeTime: anchor, Limit: 50));

		Assert.True(result.Succeeded);
		Assert.Equal(2, result.Value.Count);
		Assert.DoesNotContain(result.Value, m => m.Id == "3");
	}

	[Fact]
	public async Task NullBeforeTime_DefaultsToClockNow()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);

		var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
		h.Clock.Set(now);
		SeedMessage(h.Repository, id: 1, channelId: 100, createdAt: now.AddMinutes(-1));

		var result = await handler.HandleAsync(new GetChannelMessagesQuery(ChannelId: 100, BeforeTime: null, Limit: 50));

		Assert.True(result.Succeeded);
		Assert.Single(result.Value);
	}

	[Fact]
	public async Task Administrator_BypassesReadPermissionCheck()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: AdministratorPermission);
		h.Clock.Set(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

		var result = await handler.HandleAsync(new GetChannelMessagesQuery(ChannelId: 100, BeforeTime: null, Limit: 50));

		Assert.True(result.Succeeded);
	}
}
