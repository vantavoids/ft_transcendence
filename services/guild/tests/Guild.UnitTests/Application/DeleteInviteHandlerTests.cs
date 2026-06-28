using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Invites.DeleteInvite;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class DeleteInviteHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownCode_Returns404()
	{
		var (g, i) = NewFakes();
		var handler = NewHandler(g, i, callerId: 1);

		var result = await handler.HandleAsync(new DeleteInviteCommand(GuildId: 1, Code: "ghost"));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.InviteNotFound", result.Error.Code);
	}

	[Fact]
	public async Task AlreadyRevoked_Returns404()
	{
		var (g, i) = NewFakes();
		var guild = SeedGuild(g, ownerId: 1);
		var invite = SeedInvite(i, guild.Id, creator: 1);
		invite.Revoke();
		var handler = NewHandler(g, i, callerId: 1);

		var result = await handler.HandleAsync(new DeleteInviteCommand(guild.Id, invite.Code));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.InviteNotFound", result.Error.Code);
	}

	[Fact]
	public async Task GuildIdMismatch_Returns404()
	{
		var (g, i) = NewFakes();
		var guild = SeedGuild(g, ownerId: 1);
		var invite = SeedInvite(i, guild.Id, creator: 1);
		var handler = NewHandler(g, i, callerId: 1);

		var result = await handler.HandleAsync(new DeleteInviteCommand(GuildId: guild.Id + 1, Code: invite.Code));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.InviteGuildMismatch", result.Error.Code);
	}

	[Fact]
	public async Task NonMember_Returns403()
	{
		var (g, i) = NewFakes();
		var guild = SeedGuild(g, ownerId: 1);
		var invite = SeedInvite(i, guild.Id, creator: 1);
		var handler = NewHandler(g, i, callerId: 99);

		var result = await handler.HandleAsync(new DeleteInviteCommand(guild.Id, invite.Code));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
	}

	[Fact]
	public async Task CreatorWithoutManageGuild_CanRevoke()
	{
		var (g, i) = NewFakes();
		var guild = SeedGuild(g, ownerId: 1);
		DomainSeed.AddMember(guild, userId: 99, joinedAt: Now);
		var invite = SeedInvite(i, guild.Id, creator: 99);

		var handler = NewHandler(g, i, callerId: 99);
		var result = await handler.HandleAsync(new DeleteInviteCommand(guild.Id, invite.Code));

		Assert.True(result.Succeeded);
		Assert.True(invite.IsRevoked);
		Assert.Equal(1, i.UpdateCount);
	}

	[Fact]
	public async Task NonCreatorWithoutManageGuild_Returns403()
	{
		var (g, i) = NewFakes();
		var guild = SeedGuild(g, ownerId: 1);
		DomainSeed.AddMember(guild, userId: 99, joinedAt: Now);
		var invite = SeedInvite(i, guild.Id, creator: 1);
		var handler = NewHandler(g, i, callerId: 99);

		var result = await handler.HandleAsync(new DeleteInviteCommand(guild.Id, invite.Code));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.MissingPermission", result.Error.Code);
		Assert.False(invite.IsRevoked);
	}

	[Fact]
	public async Task OwnerCanRevoke()
	{
		var (g, i) = NewFakes();
		var guild = SeedGuild(g, ownerId: 1);
		var invite = SeedInvite(i, guild.Id, creator: 42);
		var handler = NewHandler(g, i, callerId: 1);

		var result = await handler.HandleAsync(new DeleteInviteCommand(guild.Id, invite.Code));

		Assert.True(result.Succeeded);
		Assert.True(invite.IsRevoked);
	}

	private static (FakeGuildRepository, FakeGuildInviteRepository) NewFakes()
		=> (new FakeGuildRepository(), new FakeGuildInviteRepository());

	private static ICommandHandler<DeleteInviteCommand, Result> NewHandler(
		FakeGuildRepository guilds, FakeGuildInviteRepository invites, long callerId)
	{
		return HandlerFactory.CreateCommand<DeleteInviteCommand, Result>(
			guilds, invites, new FakeCurrentUser { Id = callerId });
	}

	private static GuildEntity SeedGuild(FakeGuildRepository repo, long ownerId)
	{
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: ownerId, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		repo.Add(guild);
		return guild;
	}

	private static GuildInvite SeedInvite(FakeGuildInviteRepository repo, long guildId, long creator)
	{
		var invite = GuildInvite.Create("abc123", guildId, creator, null, null, Now).Value;
		repo.Seed(invite);
		return invite;
	}
}
