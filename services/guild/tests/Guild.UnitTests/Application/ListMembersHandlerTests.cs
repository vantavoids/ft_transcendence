using Guild.Application.Features.Membership.Common;
using Guild.Application.Features.Membership.ListMembers;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class ListMembersHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_Returns_GuildNotFound()
	{
		var (handler, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new ListMembersQuery(999, After: null, Limit: 50));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task NonMember_Returns_NotAMember()
	{
		var (handler, _) = MakeHandler(currentUser: 99);

		var result = await handler.HandleAsync(new ListMembersQuery(100, After: null, Limit: 50));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
	}

	[Fact]
	public async Task HappyPath_Returns_OnlyOwner_WhenNoOtherMembers()
	{
		var (handler, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new ListMembersQuery(100, After: null, Limit: 50));

		Assert.True(result.Succeeded);
		Assert.Single(result.Value.Items);
		Assert.Equal("1", result.Value.Items[0].UserId);
	}

	[Fact]
	public async Task After_FiltersStrictlyGreater_AndOrdersAscending()
	{
		var (handler, guilds) = MakeHandler(currentUser: 1);
		DomainSeed.AddMember(guilds.Store[100], userId: 5, joinedAt: Now);
		DomainSeed.AddMember(guilds.Store[100], userId: 7, joinedAt: Now);
		DomainSeed.AddMember(guilds.Store[100], userId: 9, joinedAt: Now);

		var result = await handler.HandleAsync(new ListMembersQuery(100, After: 5, Limit: 50));

		Assert.True(result.Succeeded);
		Assert.Equal(new[] { "7", "9" }, result.Value.Items.Select(m => m.UserId).ToArray());
	}

	[Fact]
	public async Task Limit_CapsResultCount()
	{
		var (handler, guilds) = MakeHandler(currentUser: 1);
		for (long uid = 2; uid <= 6; uid++)
			DomainSeed.AddMember(guilds.Store[100], userId: uid, joinedAt: Now);

		var result = await handler.HandleAsync(new ListMembersQuery(100, After: null, Limit: 2));

		Assert.True(result.Succeeded);
		Assert.Equal(2, result.Value.Items.Count);
		Assert.Equal("1", result.Value.Items[0].UserId);
		Assert.Equal("2", result.Value.Items[1].UserId);
	}

	[Fact]
	public async Task Includes_ExplicitRoles_OmitsEveryone()
	{
		var (handler, guilds) = MakeHandler(currentUser: 1);
		var guild = guilds.Store[100];
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		var customRole = DomainSeed.AddCustomRole(guild, roleId: 500, name: "Mod", permissions: 0, position: 5, now: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: customRole.Id, now: Now);

		var result = await handler.HandleAsync(new ListMembersQuery(100, After: null, Limit: 50));

		Assert.True(result.Succeeded);
		var member2 = result.Value.Items.Single(m => m.UserId == "2");
		Assert.Equal(new[] { "500" }, member2.Roles.ToArray());
		var owner = result.Value.Items.Single(m => m.UserId == "1");
		Assert.Single(owner.Roles);
	}

	private static (
		Guild.Application.Abstractions.Messaging.IQueryHandler<ListMembersQuery, Result<MemberListResponse>> Handler,
		FakeGuildRepository Guilds)
		MakeHandler(long currentUser)
	{
		var guilds = new FakeGuildRepository();
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		guilds.AddAsync(guild).GetAwaiter().GetResult();

		var handler = HandlerFactory.CreateQuery<ListMembersQuery, Result<MemberListResponse>>(
			guilds, new FakeCurrentUser { Id = currentUser });
		return (handler, guilds);
	}
}
