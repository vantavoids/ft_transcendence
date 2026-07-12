using System.Reflection;
using Guild.Application.Features.Guilds.Common;
using Guild.Application.Features.Guilds.UpdateGuild;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class UpdateGuildHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_Returns404Failure()
	{
		var repo = new FakeGuildRepository();
		var handler = HandlerFactory.CreateCommand<UpdateGuildCommand, Result<GuildDto>>(
			repo, new FakeEventBus(), new FakeClock(), new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(
			new UpdateGuildCommand(GuildId: 999, Name: "x", Description: null, IconUrl: null, BannerUrl: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task CallerNotMember_ReturnsNotAMember()
	{
		var repo = new FakeGuildRepository();
		Seed(repo, ownerId: 1);
		var handler = HandlerFactory.CreateCommand<UpdateGuildCommand, Result<GuildDto>>(
			repo, new FakeEventBus(), new FakeClock(), new FakeCurrentUser { Id = 99 });

		var result = await handler.HandleAsync(
			new UpdateGuildCommand(GuildId: 100, Name: "x", Description: null, IconUrl: null, BannerUrl: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
	}

	[Fact]
	public async Task CallerHasNoManageGuild_ReturnsMissingPermission()
	{
		var repo = new FakeGuildRepository();
		// create with owner=2 so the entity factory wires the admin role to user 2,
		// then swap OwnerId to 1 so user 2 is a non-owner with only the @everyone role
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 2, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		DomainSeed.SetOwnerId(guild, newOwnerId: 1);
		// strip the admin assignment so user 2 has only @everyone (no Administrator bit)
		var memberRolesField = typeof(GuildEntity).GetField("_memberRoles",
			BindingFlags.Instance | BindingFlags.NonPublic)!;
		var list = (List<MemberRole>)memberRolesField.GetValue(guild)!;
		list.Clear();
		repo.Add(guild);

		var handler = HandlerFactory.CreateCommand<UpdateGuildCommand, Result<GuildDto>>(
			repo, new FakeEventBus(), new FakeClock(), new FakeCurrentUser { Id = 2 });

		var result = await handler.HandleAsync(
			new UpdateGuildCommand(GuildId: 100, Name: "x", Description: null, IconUrl: null, BannerUrl: null));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.MissingPermission", result.Error.Code);
	}

	[Fact]
	public async Task OwnerUpdates_Succeeds_AndReturnsUpdatedDto()
	{
		var repo = new FakeGuildRepository();
		Seed(repo, ownerId: 1);
		var handler = HandlerFactory.CreateCommand<UpdateGuildCommand, Result<GuildDto>>(
			repo, new FakeEventBus(), new FakeClock(), new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(
			new UpdateGuildCommand(GuildId: 100, Name: "Renamed", Description: null, IconUrl: null, BannerUrl: null));

		Assert.True(result.Succeeded);
		Assert.Equal("Renamed", result.Value.Name);
		Assert.Equal("Renamed", repo.Store[100].Name);
	}

	[Fact]
	public async Task ManageGuildPermission_Succeeds()
	{
		var repo = new FakeGuildRepository();
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		var modRole = DomainSeed.AddCustomRole(
			guild, roleId: 200, name: "Manager", permissions: (long)Permission.ManageGuild,
			position: 2, now: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: modRole.Id, now: Now);
		repo.Add(guild);

		var handler = HandlerFactory.CreateCommand<UpdateGuildCommand, Result<GuildDto>>(
			repo, new FakeEventBus(), new FakeClock(), new FakeCurrentUser { Id = 2 });

		var result = await handler.HandleAsync(
			new UpdateGuildCommand(GuildId: 100, Name: "ByMod", Description: null, IconUrl: null, BannerUrl: null));

		Assert.True(result.Succeeded);
		Assert.Equal("ByMod", repo.Store[100].Name);
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
