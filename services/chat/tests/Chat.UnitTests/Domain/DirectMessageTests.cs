using Chat.Domain.Messages;
using Chat.Domain.Results;
using Xunit;

namespace Chat.UnitTests.Domain;

public sealed class DirectMessageTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public void Create_HappyPath_PopulatesProperties()
	{
		var result = DirectMessage.Create(
			id: 1001,
			conversationId: 42,
			senderId: 7,
			recipientId: 8,
			content: "hello world",
			replyToId: null,
			now: Now);

		Assert.True(result.Succeeded);
		var message = result.Value;

		Assert.Equal(1001L, message.Id);
		Assert.Equal(42L, message.ConversationId);
		Assert.Equal(7L, message.SenderId);
		Assert.Equal(8L, message.RecipientId);
		Assert.Equal("hello world", message.Content);
		Assert.Null(message.ReplyToId);
		Assert.Null(message.EditedAt);
		Assert.False(message.IsDeleted);
		Assert.Equal(Now, message.CreatedAt);
	}

	[Fact]
	public void Create_WithReplyToId_PersistsReplyToId()
	{
		var result = DirectMessage.Create(
			id: 1001, conversationId: 42, senderId: 7, recipientId: 8,
			content: "reply", replyToId: 999, now: Now);

		Assert.True(result.Succeeded);
		Assert.Equal(999L, result.Value.ReplyToId);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("\t\n")]
	public void Create_BlankContent_ReturnsContentRequired(string? content)
	{
		var result = DirectMessage.Create(
			id: 1, conversationId: 1, senderId: 1, recipientId: 1,
			content: content, replyToId: null, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(DirectMessageFailures.ContentRequired, result.Error);
	}

	[Fact]
	public void Create_ContentTooLong_ReturnsContentTooLong()
	{
		var content = new string('x', Message.MaxContentLen + 1);

		var result = DirectMessage.Create(
			id: 1, conversationId: 1, senderId: 1, recipientId: 1,
			content: content, replyToId: null, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(DirectMessageFailures.ContentTooLong, result.Error);
	}

	[Fact]
	public void Create_ContentExactlyAtMax_Succeeds()
	{
		var content = new string('x', DirectMessage.MaxContentLen);

		var result = DirectMessage.Create(
			id: 1, conversationId: 1, senderId: 1, recipientId: 1,
			content: content, replyToId: null, now: Now);

		Assert.True(result.Succeeded);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void Create_NonPositiveId_ReturnsInvalidId(long id)
	{
		var result = DirectMessage.Create(
			id: id, conversationId: 1, senderId: 1, recipientId: 1,
			content: "hi", replyToId: null, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(DirectMessageFailures.InvalidId, result.Error);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void Create_NonPositiveChannelId_ReturnsInvalidConversationId(long conversationId)
	{
		var result = DirectMessage.Create(
			id: 1, conversationId: conversationId, senderId: 1, recipientId: 1,
			content: "hi", replyToId: null, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(DirectMessageFailures.InvalidConversationId, result.Error);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void Create_NonPositiveAuthorId_ReturnsInvalidSenderId(long senderId)
	{
		var result = DirectMessage.Create(
			id: 1, conversationId: 1, senderId: senderId, recipientId: 1,
			content: "hi", replyToId: null, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(DirectMessageFailures.InvalidSenderId, result.Error);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void Create_NonPositiveAuthorId_ReturnsInvalidRecipientId(long recipientId)
	{
		var result = DirectMessage.Create(
			id: 1, conversationId: 1, senderId: 1, recipientId: recipientId,
			content: "hi", replyToId: null, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(DirectMessageFailures.InvalidSenderId, result.Error);
	}
}
