using Chat.Domain.Messages;
using Chat.Domain.Results;
using Xunit;

namespace Chat.UnitTests.Domain;

public sealed class MessageTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public void Create_HappyPath_PopulatesProperties()
	{
		var result = Message.CreateForChannel(
			id: 1001,
			channelId: 42,
			authorId: 7,
			content: "hello world",
			replyToId: null,
			now: Now);

		Assert.True(result.Succeeded);
		var message = result.Value;

		Assert.Equal(1001L, message.Id);
		Assert.Equal(42L, message.ContainerId);
		Assert.Equal(7L, message.AuthorId);
		Assert.Equal("hello world", message.Content);
		Assert.Null(message.ReplyToId);
		Assert.Null(message.EditedAt);
		Assert.False(message.IsDeleted);
		Assert.Equal(Now, message.CreatedAt);
	}

	[Fact]
	public void Create_WithReplyToId_PersistsReplyToId()
	{
		var result = Message.CreateForChannel(
			id: 1001, channelId: 42, authorId: 7,
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
		var result = Message.CreateForChannel(
			id: 1, channelId: 1, authorId: 1,
			content: content, replyToId: null, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.ContentRequired, result.Error);
	}

	[Fact]
	public void Create_ContentTooLong_ReturnsContentTooLong()
	{
		var content = new string('x', Message.MaxContentLen + 1);

		var result = Message.CreateForChannel(
			id: 1, channelId: 1, authorId: 1,
			content: content, replyToId: null, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.ContentTooLong, result.Error);
	}

	[Fact]
	public void Create_ContentExactlyAtMax_Succeeds()
	{
		var content = new string('x', Message.MaxContentLen);

		var result = Message.CreateForChannel(
			id: 1, channelId: 1, authorId: 1,
			content: content, replyToId: null, now: Now);

		Assert.True(result.Succeeded);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void Create_NonPositiveId_ReturnsInvalidId(long id)
	{
		var result = Message.CreateForChannel(
			id: id, channelId: 1, authorId: 1,
			content: "hi", replyToId: null, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.InvalidId, result.Error);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void Create_NonPositiveChannelId_ReturnsInvalidChannelId(long channelId)
	{
		var result = Message.CreateForChannel(
			id: 1, channelId: channelId, authorId: 1,
			content: "hi", replyToId: null, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.InvalidChannelId, result.Error);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void Create_NonPositiveAuthorId_ReturnsInvalidAuthorId(long authorId)
	{
		var result = Message.CreateForChannel(
			id: 1, channelId: 1, authorId: authorId,
			content: "hi", replyToId: null, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.InvalidAuthorId, result.Error);
	}
}
