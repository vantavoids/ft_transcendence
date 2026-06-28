using System.Reflection;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Contracts;
using Guild.Application.Features.Invites.Common;
using Guild.Application.Features.Invites.CreateInvite;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class CreateInviteHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_Returns404()
	{
		var (g, i, b) = NewFakes();
		var handler = NewHandler(g, i, b, callerId: 1);

		var result = await handler.HandleAsync(new CreateInviteCommand(GuildId: 9999, MaxUses: null, ExpiresInHours: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
		Assert.Equal(0, i.AddCount);
		Assert.Empty(b.Published);
	}

	[Fact]
	public async Task NonMember_ReturnsNotAMember()
	{
		var (g, i, b) = NewFakes();
		var guild = Seed(g, ownerId: 1);
		var handler = NewHandler(g, i, b, callerId: 99);

		var result = await handler.HandleAsync(new CreateInviteCommand(GuildId: guild.Id, MaxUses: null, ExpiresInHours: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
	}

	[Fact]
	public async Task MemberWithoutCreateInvite_Returns403()
	{
		var (g, i, b) = NewFakes();
		var guild = Seed(g, ownerId: 1);
		DomainSeed.AddMember(guild, userId: 99, joinedAt: Now);
		// strip @everyone of CREATE_INVITE so the member cannot create invites
		var everyone = guild.Roles.Single(r => r.IsDefault);
		DomainSeed.SetRolePermissions(everyone, 0);
		var handler = NewHandler(g, i, b, callerId: 99);

		var result = await handler.HandleAsync(new CreateInviteCommand(GuildId: guild.Id, MaxUses: null, ExpiresInHours: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.MissingPermission", result.Error.Code);
		Assert.Equal(0, i.AddCount);
	}

	[Fact]
	public async Task HappyPath_CreatesInviteAndPublishesEvent()
	{
		var (g, i, b) = NewFakes();
		var guild = Seed(g, ownerId: 1);
		var handler = NewHandler(g, i, b, callerId: 1, codeSeed: "abcdef");

		var result = await handler.HandleAsync(
			new CreateInviteCommand(GuildId: guild.Id, MaxUses: 10, ExpiresInHours: 24));

		Assert.True(result.Succeeded);
		Assert.Equal("abcdef01", result.Value.Code);
		Assert.Equal(guild.Id.ToString(), result.Value.GuildId);
		Assert.Equal("1", result.Value.CreatedBy);
		Assert.Equal(10, result.Value.MaxUses);
		Assert.NotNull(result.Value.ExpiresAt);

		Assert.Equal(1, i.AddCount);

		var evt = b.Single<GuildInviteCreated>();
		Assert.Equal(guild.Id, evt.GuildId);
		Assert.Equal("Test", evt.GuildName);
		Assert.Equal(1L, evt.InvitedByUserId);
		Assert.Null(evt.InvitedUserId);
	}

	[Fact]
	public async Task NoExpiry_NullExpiresAt()
	{
		var (g, i, b) = NewFakes();
		var guild = Seed(g, ownerId: 1);
		var handler = NewHandler(g, i, b, callerId: 1);

		var result = await handler.HandleAsync(
			new CreateInviteCommand(GuildId: guild.Id, MaxUses: null, ExpiresInHours: null));

		Assert.True(result.Succeeded);
		Assert.Null(result.Value.ExpiresAt);
		Assert.Null(result.Value.MaxUses);
	}

	[Fact]
	public async Task NegativeMaxUses_PropagatesDomainFailure()
	{
		var (g, i, b) = NewFakes();
		var guild = Seed(g, ownerId: 1);
		var handler = NewHandler(g, i, b, callerId: 1);

		var result = await handler.HandleAsync(
			new CreateInviteCommand(GuildId: guild.Id, MaxUses: -5, ExpiresInHours: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.InviteMaxUsesInvalid", result.Error.Code);
		Assert.Equal(0, i.AddCount);
	}

	private static (FakeGuildRepository, FakeGuildInviteRepository, FakeEventBus) NewFakes()
		=> (new FakeGuildRepository(), new FakeGuildInviteRepository(), new FakeEventBus());

	private static ICommandHandler<CreateInviteCommand, Result<InviteDto>> NewHandler(
		FakeGuildRepository guilds,
		FakeGuildInviteRepository invites,
		FakeEventBus bus,
		long callerId,
		string codeSeed = "abcdef")
	{
		return HandlerFactory.CreateCommand<CreateInviteCommand, Result<InviteDto>>(
			guilds, invites, new FakeInviteCodeGenerator(codeSeed), bus,
			new FakeClock(), new FakeCurrentUser { Id = callerId });
	}

	private static GuildEntity Seed(FakeGuildRepository repo, long ownerId)
	{
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: ownerId, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		repo.Add(guild);
		return guild;
	}
}
