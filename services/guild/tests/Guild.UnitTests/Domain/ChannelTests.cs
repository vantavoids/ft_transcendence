using Guild.Domain.Guild;
using Guild.Domain.Results;
using Xunit;

namespace Guild.UnitTests.Domain;

public sealed class ChannelTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public void Create_HappyPath_PersistsFields()
	{
		var result = Channel.Create(
			id: 1, guildId: 10, categoryId: 7,
			name: "general", topic: "general chat",
			type: ChannelType.Text, position: 2, now: Now);

		Assert.True(result.Succeeded);
		var channel = result.Value;
		Assert.Equal(1L, channel.Id);
		Assert.Equal(10L, channel.GuildId);
		Assert.Equal(7L, channel.CategoryId);
		Assert.Equal("general", channel.Name);
		Assert.Equal("general chat", channel.Topic);
		Assert.Equal(ChannelType.Text, channel.Type);
		Assert.Equal(2, channel.Position);
		Assert.Equal(Now, channel.CreatedAt);
		Assert.Equal(Now, channel.UpdatedAt);
	}

	[Fact]
	public void Create_NullCategory_IsAllowed()
	{
		var result = Channel.Create(
			id: 1, guildId: 10, categoryId: null,
			name: "general", topic: null,
			type: ChannelType.Text, position: 0, now: Now);

		Assert.True(result.Succeeded);
		Assert.Null(result.Value.CategoryId);
		Assert.Null(result.Value.Topic);
	}

	[Fact]
	public void Create_EmptyName_Fails()
	{
		var result = Channel.Create(
			id: 1, guildId: 10, categoryId: null,
			name: "", topic: null, type: ChannelType.Text, position: 0, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.ChannelNameRequired, result.Error);
	}

	[Fact]
	public void Create_NullName_Fails()
	{
		var result = Channel.Create(
			id: 1, guildId: 10, categoryId: null,
			name: null, topic: null, type: ChannelType.Text, position: 0, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.ChannelNameRequired, result.Error);
	}

	[Fact]
	public void Create_NameTooLong_Fails()
	{
		var name = new string('x', Channel.MaxNameLen + 1);

		var result = Channel.Create(
			id: 1, guildId: 10, categoryId: null,
			name: name, topic: null, type: ChannelType.Text, position: 0, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.ChannelNameTooLong, result.Error);
	}

	[Fact]
	public void Create_TopicTooLong_Fails()
	{
		var topic = new string('x', Channel.MaxTopicLen + 1);

		var result = Channel.Create(
			id: 1, guildId: 10, categoryId: null,
			name: "general", topic: topic, type: ChannelType.Text, position: 0, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.ChannelTopicTooLong, result.Error);
	}

	[Fact]
	public void Create_InvalidType_Fails()
	{
		var result = Channel.Create(
			id: 1, guildId: 10, categoryId: null,
			name: "general", topic: null,
			type: (ChannelType)999, position: 0, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.ChannelInvalidType, result.Error);
	}

	[Fact]
	public void Rename_HappyPath()
	{
		var channel = CreateValid();
		var later = Now.AddHours(1);

		var result = channel.Rename("new-name", later);

		Assert.True(result.Succeeded);
		Assert.Equal("new-name", channel.Name);
		Assert.Equal(later, channel.UpdatedAt);
	}

	[Fact]
	public void Rename_Invalid_Fails()
	{
		var channel = CreateValid();
		var result = channel.Rename("", Now);
		Assert.True(result.IsFailure);
	}

	[Fact]
	public void UpdateTopic_EmptyString_ClearsTopic()
	{
		var channel = CreateValid();
		channel.UpdateTopic("some topic", Now);

		var result = channel.UpdateTopic("", Now.AddHours(1));

		Assert.True(result.Succeeded);
		Assert.Null(channel.Topic);
	}

	[Fact]
	public void Reposition_UpdatesPosition()
	{
		var channel = CreateValid();
		var later = Now.AddHours(2);

		var result = channel.Reposition(7, later);

		Assert.True(result.Succeeded);
		Assert.Equal(7, channel.Position);
		Assert.Equal(later, channel.UpdatedAt);
	}

	[Fact]
	public void Reposition_NegativeClampsToZero()
	{
		var channel = CreateValid();
		var result = channel.Reposition(-3, Now);

		Assert.True(result.Succeeded);
		Assert.Equal(0, channel.Position);
	}

	[Fact]
	public void MoveToCategory_NullDetaches()
	{
		var channel = CreateValid();
		var later = Now.AddHours(3);

		var result = channel.MoveToCategory(null, later);

		Assert.True(result.Succeeded);
		Assert.Null(channel.CategoryId);
		Assert.Equal(later, channel.UpdatedAt);
	}

	[Fact]
	public void MoveToCategory_NonNull_Attaches()
	{
		var channel = CreateValid();
		var result = channel.MoveToCategory(42L, Now.AddHours(1));

		Assert.True(result.Succeeded);
		Assert.Equal(42L, channel.CategoryId);
	}

	private static Channel CreateValid() =>
		Channel.Create(
			id: 1, guildId: 10, categoryId: 5,
			name: "general", topic: null, type: ChannelType.Text,
			position: 0, now: Now).Value;
}
