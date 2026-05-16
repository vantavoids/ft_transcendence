using Guild.Application.Features.Guilds.CreateGuild;
using Guild.Application.Features.Guilds.Common;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;

namespace Guild.UnitTests.Application;

public sealed class CreateGuildHandlerTests
{
	[Fact]
	public async Task HappyPath_PersistsAndReturnsDto()
	{
		var repo = new FakeGuildRepository();
		var ids = new FakeIdGenerator();
		var clock = new FakeClock();
		var user = new FakeCurrentUser { Id = 42 };
		var handler = HandlerFactory.CreateCommand<CreateGuildCommand, Result<GuildDto>>(repo, ids, clock, user);

		var result = await handler.HandleAsync(
			new CreateGuildCommand(Name: "Hello", Description: "hi", IconUrl: null, BannerUrl: null));

		Assert.True(result.Succeeded);
		var dto = result.Value;

		Assert.Equal("42", dto.OwnerId);
		Assert.Equal("Hello", dto.Name);
		Assert.True(long.TryParse(dto.Id, out _));
		Assert.Single(repo.Store);
	}

	[Fact]
	public async Task InvalidName_ReturnsFailure_DoesNotPersist()
	{
		var repo = new FakeGuildRepository();
		var handler = HandlerFactory.CreateCommand<CreateGuildCommand, Result<GuildDto>>(
			repo, new FakeIdGenerator(), new FakeClock(), new FakeCurrentUser { Id = 42 });

		var result = await handler.HandleAsync(
			new CreateGuildCommand(Name: null, Description: null, IconUrl: null, BannerUrl: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNameRequired", result.Error.Code);
		Assert.Empty(repo.Store);
	}

	[Fact]
	public async Task NameTooLong_ReturnsFailure_DoesNotPersist()
	{
		var repo = new FakeGuildRepository();
		var handler = HandlerFactory.CreateCommand<CreateGuildCommand, Result<GuildDto>>(
			repo, new FakeIdGenerator(), new FakeClock(), new FakeCurrentUser { Id = 42 });

		var name = new string('x', 101);
		var result = await handler.HandleAsync(
			new CreateGuildCommand(Name: name, Description: null, IconUrl: null, BannerUrl: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNameTooLong", result.Error.Code);
		Assert.Empty(repo.Store);
	}
}
