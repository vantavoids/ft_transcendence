using Guild.Application.Features.Guilds.Common;
using Guild.Application.Features.Guilds.TransferOwnership;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class TransferOwnershipHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_Returns404()
	{
		var repo = new FakeGuildRepository();
		var handler = HandlerFactory.CreateCommand<TransferOwnershipCommand, Result<GuildDto>>(
			repo, new FakeClock(), new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(new TransferOwnershipCommand(GuildId: 999, NewOwnerId: 2));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task NonOwner_ReturnsNotTheOwner()
	{
		var repo = new FakeGuildRepository();
		Seed(repo, ownerId: 1);
		var handler = HandlerFactory.CreateCommand<TransferOwnershipCommand, Result<GuildDto>>(
			repo, new FakeClock(), new FakeCurrentUser { Id = 99 });

		var result = await handler.HandleAsync(new TransferOwnershipCommand(GuildId: 100, NewOwnerId: 2));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotTheOwner", result.Error.Code);
	}

	[Fact]
	public async Task TargetNotMember_ReturnsTargetNotAMember()
	{
		var repo = new FakeGuildRepository();
		Seed(repo, ownerId: 1);
		var handler = HandlerFactory.CreateCommand<TransferOwnershipCommand, Result<GuildDto>>(
			repo, new FakeClock(), new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(new TransferOwnershipCommand(GuildId: 100, NewOwnerId: 99));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.TargetNotAMember", result.Error.Code);
	}

	[Fact]
	public async Task HappyPath_OwnerChanges()
	{
		var repo = new FakeGuildRepository();
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		await repo.AddAsync(guild);

		var handler = HandlerFactory.CreateCommand<TransferOwnershipCommand, Result<GuildDto>>(
			repo, new FakeClock(), new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(new TransferOwnershipCommand(GuildId: 100, NewOwnerId: 2));

		Assert.True(result.Succeeded);
		Assert.Equal("2", result.Value.OwnerId);
		Assert.Equal(2L, repo.Store[100].OwnerId);
		Assert.Equal(1, repo.UpdateCount);
		Assert.Equal(1, repo.SaveChangesCount);
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
		repo.AddAsync(guild).GetAwaiter().GetResult();
	}
}
