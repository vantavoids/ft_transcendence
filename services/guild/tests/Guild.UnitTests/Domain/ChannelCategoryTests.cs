using Guild.Domain.Guild;
using Guild.Domain.Results;
using Xunit;

namespace Guild.UnitTests.Domain;

public sealed class ChannelCategoryTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public void Create_HappyPath_PersistsFieldsAndTimestamps()
	{
		var result = ChannelCategory.Create(
			id: 1, guildId: 10, name: "Text", position: 3, now: Now);

		Assert.True(result.Succeeded);
		var category = result.Value;
		Assert.Equal(1L, category.Id);
		Assert.Equal(10L, category.GuildId);
		Assert.Equal("Text", category.Name);
		Assert.Equal(3, category.Position);
		Assert.Equal(Now, category.CreatedAt);
		Assert.Equal(Now, category.UpdatedAt);
	}

	[Fact]
	public void Create_NullName_Fails()
	{
		var result = ChannelCategory.Create(
			id: 1, guildId: 10, name: null, position: 0, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.CategoryNameRequired, result.Error);
	}

	[Fact]
	public void Create_EmptyName_Fails()
	{
		var result = ChannelCategory.Create(
			id: 1, guildId: 10, name: "", position: 0, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.CategoryNameRequired, result.Error);
	}

	[Fact]
	public void Create_WhitespaceName_Fails()
	{
		var result = ChannelCategory.Create(
			id: 1, guildId: 10, name: "   ", position: 0, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.CategoryNameRequired, result.Error);
	}

	[Fact]
	public void Create_NameTooLong_Fails()
	{
		var name = new string('x', ChannelCategory.MaxNameLen + 1);

		var result = ChannelCategory.Create(
			id: 1, guildId: 10, name: name, position: 0, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.CategoryNameTooLong, result.Error);
	}

	[Fact]
	public void Rename_HappyPath_UpdatesNameAndTimestamp()
	{
		var category = CreateValid();
		var later = Now.AddHours(1);

		var result = category.Rename("New name", later);

		Assert.True(result.Succeeded);
		Assert.Equal("New name", category.Name);
		Assert.Equal(later, category.UpdatedAt);
	}

	[Fact]
	public void Rename_EmptyName_Fails()
	{
		var category = CreateValid();

		var result = category.Rename("", Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.CategoryNameRequired, result.Error);
	}

	[Fact]
	public void Rename_TooLongName_Fails()
	{
		var category = CreateValid();
		var name = new string('a', ChannelCategory.MaxNameLen + 1);

		var result = category.Rename(name, Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.CategoryNameTooLong, result.Error);
	}

	[Fact]
	public void Create_NameAtMaxLength_Succeeds()
	{
		var name = new string('x', ChannelCategory.MaxNameLen);

		var result = ChannelCategory.Create(id: 1, guildId: 10, name: name, position: 0, now: Now);

		Assert.True(result.Succeeded);
	}

	[Fact]
	public void Rename_NameAtMaxLength_Succeeds()
	{
		var category = CreateValid();
		var name = new string('x', ChannelCategory.MaxNameLen);

		var result = category.Rename(name, Now);

		Assert.True(result.Succeeded);
		Assert.Equal(name, category.Name);
	}

	[Fact]
	public void Reposition_UpdatesPositionAndTimestamp()
	{
		var category = CreateValid();
		var later = Now.AddHours(2);

		var result = category.Reposition(7, later);

		Assert.True(result.Succeeded);
		Assert.Equal(7, category.Position);
		Assert.Equal(later, category.UpdatedAt);
	}

	private static ChannelCategory CreateValid() =>
		ChannelCategory.Create(id: 1, guildId: 10, name: "Initial", position: 0, now: Now).Value;
}
