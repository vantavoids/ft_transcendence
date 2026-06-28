using Guild.Application.Features.Roles.Common;
using Guild.Application.Features.Roles.ListRoles;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class ListRolesHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_Returns_GuildNotFound()
	{
		var (handler, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new ListRolesQuery(999));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task NonMember_Returns_NotAMember()
	{
		var (handler, _) = MakeHandler(currentUser: 99);

		var result = await handler.HandleAsync(new ListRolesQuery(100));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
	}

	[Fact]
	public async Task HappyPath_Returns_DefaultRoles_OrderedByPositionAscending()
	{
		var (handler, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new ListRolesQuery(100));

		Assert.True(result.Succeeded);
		Assert.Equal(2, result.Value.Items.Count);
		Assert.Equal("@everyone", result.Value.Items[0].Name);
		Assert.True(result.Value.Items[0].IsDefault);
		Assert.Equal("Administrator", result.Value.Items[1].Name);
		Assert.False(result.Value.Items[1].IsDefault);
	}

	[Fact]
	public async Task Includes_Custom_Roles_Sorted_By_Position()
	{
		var (handler, guilds) = MakeHandler(currentUser: 1);
		var guild = guilds.Store[100];
		DomainSeed.AddCustomRole(guild, roleId: 500, name: "Z-Role", permissions: 0, position: 5, now: Now);
		DomainSeed.AddCustomRole(guild, roleId: 501, name: "A-Role", permissions: 0, position: 3, now: Now);

		var result = await handler.HandleAsync(new ListRolesQuery(100));

		Assert.True(result.Succeeded);
		var positions = result.Value.Items.Select(r => r.Position).ToArray();
		Assert.Equal(new[] { 0, 1, 3, 5 }, positions);
	}

	private static (
		Guild.Application.Abstractions.Messaging.IQueryHandler<ListRolesQuery, Result<RoleListResponse>> Handler,
		FakeGuildRepository Guilds)
		MakeHandler(long currentUser)
	{
		var guilds = new FakeGuildRepository();
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		guilds.Add(guild);
		var handler = HandlerFactory.CreateQuery<ListRolesQuery, Result<RoleListResponse>>(
			guilds, new FakeCurrentUser { Id = currentUser });
		return (handler, guilds);
	}
}
