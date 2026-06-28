using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Membership.GetMemberPermissions;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class GetMemberPermissionsHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_Returns_GuildNotFound()
	{
		var (handler, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new GetMemberPermissionsQuery(999, TargetUserId: 1));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task CallerNotMember_Returns_NotAMember()
	{
		var (handler, _) = MakeHandler(currentUser: 99);

		var result = await handler.HandleAsync(new GetMemberPermissionsQuery(100, TargetUserId: 1));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
	}

	[Fact]
	public async Task TargetNotAMember_Returns_TargetNotAMember()
	{
		var (handler, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new GetMemberPermissionsQuery(100, TargetUserId: 555));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.TargetNotAMember", result.Error.Code);
	}

	[Fact]
	public async Task Owner_ReportsIsOwner_WithAdministrator()
	{
		var (handler, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new GetMemberPermissionsQuery(100, TargetUserId: 1));

		Assert.True(result.Succeeded);
		Assert.True(result.Value.IsOwner);
		Assert.NotEqual(0, result.Value.EffectivePermissions & (long)Permission.Administrator);
	}

	[Fact]
	public async Task Member_ReportsAssignedRolePermissions_WithoutOwnerFlag()
	{
		var (handler, guilds) = MakeHandler(currentUser: 1);
		var guild = guilds.Store[100];
		DomainSeed.AddCustomRole(guild, roleId: 500, name: "Mod",
			permissions: (long)Permission.BanMembers, position: 5, now: Now);
		DomainSeed.AddMember(guild, userId: 3, joinedAt: Now);
		DomainSeed.AssignRole(guild, userId: 3, roleId: 500, now: Now);

		var result = await handler.HandleAsync(new GetMemberPermissionsQuery(100, TargetUserId: 3));

		Assert.True(result.Succeeded);
		Assert.False(result.Value.IsOwner);
		Assert.NotEqual(0, result.Value.EffectivePermissions & (long)Permission.BanMembers);
		Assert.Contains(result.Value.Roles, r => r.Id == "500");
	}

	private static (IQueryHandler<GetMemberPermissionsQuery, Result<MemberPermissionsResponse>> Handler, FakeGuildRepository Guilds)
		MakeHandler(long currentUser)
	{
		var guilds = new FakeGuildRepository();
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		guilds.AddAsync(guild).GetAwaiter().GetResult();

		var handler = HandlerFactory.CreateQuery<GetMemberPermissionsQuery, Result<MemberPermissionsResponse>>(
			guilds, new FakeCurrentUser { Id = currentUser });
		return (handler, guilds);
	}
}
