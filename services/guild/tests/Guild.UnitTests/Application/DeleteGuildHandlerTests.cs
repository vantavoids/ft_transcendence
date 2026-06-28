using Guild.Application.Features.Guilds.DeleteGuild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class DeleteGuildHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_Returns404()
	{
		var repo = new FakeGuildRepository();
		var handler = HandlerFactory.CreateCommand<DeleteGuildCommand, Result>(
			repo, new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(new DeleteGuildCommand(GuildId: 999));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task NonOwner_ReturnsNotTheOwner()
	{
		var repo = new FakeGuildRepository();
		Seed(repo, ownerId: 1);
		var handler = HandlerFactory.CreateCommand<DeleteGuildCommand, Result>(
			repo, new FakeCurrentUser { Id = 99 });

		var result = await handler.HandleAsync(new DeleteGuildCommand(GuildId: 100));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotTheOwner", result.Error.Code);
	}

	[Fact]
	public async Task Owner_Succeeds_RemovesFromStore()
	{
		var repo = new FakeGuildRepository();
		Seed(repo, ownerId: 1);
		var handler = HandlerFactory.CreateCommand<DeleteGuildCommand, Result>(
			repo, new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(new DeleteGuildCommand(GuildId: 100));

		Assert.True(result.Succeeded);
		Assert.Empty(repo.Store);
	}

	private static void Seed(FakeGuildRepository repo, long ownerId)
	{
		var guild = GuildEntity.Create(
			id: 100,
			name: "Test",
			description: null,
			iconUrl: null,
			bannerUrl: null,
			ownerId: ownerId,
			everyoneRoleId: 101,
			adminRoleId: 102,
			now: Now).Value;
		repo.Add(guild);
	}
}
