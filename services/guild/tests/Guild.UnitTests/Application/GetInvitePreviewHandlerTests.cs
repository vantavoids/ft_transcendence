using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Invites.Common;
using Guild.Application.Features.Invites.GetInvitePreview;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class GetInvitePreviewHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownCode_Returns404()
	{
		var (g, i, u) = NewFakes();
		var handler = NewHandler(g, i, u);

		var result = await handler.HandleAsync(new GetInvitePreviewQuery("ghost"));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.InviteNotFound", result.Error.Code);
	}

	[Fact]
	public async Task RevokedInvite_Returns404()
	{
		var (g, i, u) = NewFakes();
		var guild = SeedGuild(g);
		var invite = SeedInvite(i, guildId: guild.Id, creator: 1);
		invite.Revoke();
		var handler = NewHandler(g, i, u);

		var result = await handler.HandleAsync(new GetInvitePreviewQuery(invite.Code));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.InviteNotFound", result.Error.Code);
	}

	[Fact]
	public async Task ExpiredInvite_Returns404()
	{
		var (g, i, u) = NewFakes();
		var guild = SeedGuild(g);
		var invite = GuildInvite.Create("abc", guild.Id, 1, null, Now.AddHours(1), Now).Value;
		i.Seed(invite);

		var handler = HandlerFactory.CreateQuery<GetInvitePreviewQuery, Result<InvitePreviewDto>>(
			g, i, u, new FakeClock(Now.AddHours(2)));

		var result = await handler.HandleAsync(new GetInvitePreviewQuery(invite.Code));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.InviteNotFound", result.Error.Code);
	}

	[Fact]
	public async Task HappyPath_ReturnsPreview_WithInviterFromUserService()
	{
		var (g, i, u) = NewFakes();
		var guild = SeedGuild(g);
		var invite = SeedInvite(i, guildId: guild.Id, creator: 42, expiresAt: Now.AddHours(1));
		u.RegisterSummary(42, "yandry");

		var handler = NewHandler(g, i, u);
		var result = await handler.HandleAsync(new GetInvitePreviewQuery(invite.Code));

		Assert.True(result.Succeeded);
		var dto = result.Value;
		Assert.Equal("abc123", dto.Code);
		Assert.Equal(guild.Id.ToString(), dto.Guild.Id);
		Assert.Equal("Test", dto.Guild.Name);
		Assert.Equal(1, dto.Guild.MemberCount);
		Assert.Equal("42", dto.Inviter.Id);
		Assert.Equal("yandry", dto.Inviter.Username);
		Assert.NotNull(dto.ExpiresAt);
		Assert.Equal(1, u.GetSummaryCallCount);
	}

	[Fact]
	public async Task UserServiceMisses_FallsBackToEmptyUsername()
	{
		var (g, i, u) = NewFakes();
		var guild = SeedGuild(g);
		var invite = SeedInvite(i, guildId: guild.Id, creator: 42);

		var handler = NewHandler(g, i, u);
		var result = await handler.HandleAsync(new GetInvitePreviewQuery(invite.Code));

		Assert.True(result.Succeeded);
		Assert.Equal("42", result.Value.Inviter.Id);
		Assert.Equal(string.Empty, result.Value.Inviter.Username);
	}

	[Fact]
	public async Task GuildMissing_Returns404()
	{
		var (g, i, u) = NewFakes();
		var invite = SeedInvite(i, guildId: 9999, creator: 1);
		var handler = NewHandler(g, i, u);

		var result = await handler.HandleAsync(new GetInvitePreviewQuery(invite.Code));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.InviteNotFound", result.Error.Code);
	}

	private static (FakeGuildRepository, FakeGuildInviteRepository, FakeUserService) NewFakes()
		=> (new FakeGuildRepository(), new FakeGuildInviteRepository(), new FakeUserService());

	private static IQueryHandler<GetInvitePreviewQuery, Result<InvitePreviewDto>> NewHandler(
		FakeGuildRepository guilds, FakeGuildInviteRepository invites, FakeUserService users)
	{
		return HandlerFactory.CreateQuery<GetInvitePreviewQuery, Result<InvitePreviewDto>>(
			guilds, invites, users, new FakeClock());
	}

	private static GuildEntity SeedGuild(FakeGuildRepository repo)
	{
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		repo.AddAsync(guild).GetAwaiter().GetResult();
		return guild;
	}

	private static GuildInvite SeedInvite(
		FakeGuildInviteRepository repo, long guildId, long creator, DateTimeOffset? expiresAt = null)
	{
		var invite = GuildInvite.Create("abc123", guildId, creator, null, expiresAt, Now).Value;
		repo.Seed(invite);
		return invite;
	}
}
