using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Bans.UnbanMember;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class UnbanMemberHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_Returns_GuildNotFound()
	{
		var (handler, _, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new UnbanMemberCommand(999, TargetUserId: 2));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task CallerNotMember_Returns_NotAMember()
	{
		var (handler, _, _) = MakeHandler(currentUser: 99);

		var result = await handler.HandleAsync(new UnbanMemberCommand(100, TargetUserId: 2));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
	}

	[Fact]
	public async Task CallerWithoutBanMembers_Returns_MissingPermission()
	{
		var (handler, guilds, bans) = MakeHandler(currentUser: 2);
		DomainSeed.AddMember(guilds.Store[100], userId: 2, joinedAt: Now);
		bans.Seed(GuildBan.Create(guildId: 100, userId: 3, bannedBy: 1, reason: null, now: Now).Value);

		var result = await handler.HandleAsync(new UnbanMemberCommand(100, TargetUserId: 3));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.MissingPermission", result.Error.Code);
	}

	[Fact]
	public async Task NoSuchBan_Returns_BanNotFound()
	{
		var (handler, _, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new UnbanMemberCommand(100, TargetUserId: 3));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.BanNotFound", result.Error.Code);
	}

	[Fact]
	public async Task NegativeTargetId_Returns_BanNotFound()
	{
		// characterization of the known ID-validation gap: there is no invariant
		// check on TargetUserId, so a negative id simply finds no ban and the
		// handler emits BanNotFound rather than InvalidId (see cleanup plan 0.3)
		var (handler, _, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new UnbanMemberCommand(100, TargetUserId: -1));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.BanNotFound", result.Error.Code);
	}

	[Fact]
	public async Task HappyPath_RemovesBan()
	{
		var (handler, _, bans) = MakeHandler(currentUser: 1);
		bans.Seed(GuildBan.Create(guildId: 100, userId: 3, bannedBy: 1, reason: null, now: Now).Value);

		var result = await handler.HandleAsync(new UnbanMemberCommand(100, TargetUserId: 3));

		Assert.True(result.Succeeded);
		Assert.False(bans.Store.ContainsKey((100, 3)));
	}

	private static (
		ICommandHandler<UnbanMemberCommand, Result> Handler,
		FakeGuildRepository Guilds,
		FakeGuildBanRepository Bans)
		MakeHandler(long currentUser)
	{
		var guilds = new FakeGuildRepository();
		var bans = new FakeGuildBanRepository();
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		guilds.AddAsync(guild).GetAwaiter().GetResult();

		var handler = HandlerFactory.CreateCommand<UnbanMemberCommand, Result>(
			guilds, bans, new FakeCurrentUser { Id = currentUser });
		return (handler, guilds, bans);
	}
}
