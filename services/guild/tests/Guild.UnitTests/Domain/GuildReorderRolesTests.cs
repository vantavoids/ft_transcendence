using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Domain;

public sealed class GuildReorderRolesTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	// guild seeded with @everyone(id 101, pos 0) + Administrator(id 102, pos 1)
	// by Create, plus two custom roles at pos 2 and 3. non-default roles are
	// {102, 500, 501}. reorder rearranges a subset among the slots they occupy
	private static GuildEntity MakeGuild()
	{
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		DomainSeed.AddCustomRole(guild, roleId: 500, name: "A", permissions: 0, position: 2, now: Now);
		DomainSeed.AddCustomRole(guild, roleId: 501, name: "B", permissions: 0, position: 3, now: Now);
		return guild;
	}

	[Fact]
	public void SwapSubset_ReordersAndReturnsDescending()
	{
		var guild = MakeGuild();

		// swap A(pos 2) and B(pos 3); admin(pos 1) is untouched and not in the body
		var result = guild.ReorderRoles([(500, 3), (501, 2)], Now);

		Assert.True(result.Succeeded);
		Assert.Equal(3, guild.Roles.Single(r => r.Id == 500).Position);
		Assert.Equal(2, guild.Roles.Single(r => r.Id == 501).Position);
		Assert.Equal(1, guild.Roles.Single(r => r.Id == 102).Position); // untouched
		Assert.Equal(0, guild.Roles.Single(r => r.Id == 101).Position); // @everyone pinned
		// response ordered by position descending (incl. @everyone last)
		Assert.Equal(new long[] { 500, 501, 102, 101 }, result.Value.Select(r => r.Id).ToArray());
	}

	[Fact]
	public void SamePosition_IsNoOp_Succeeds()
	{
		var guild = MakeGuild();

		var result = guild.ReorderRoles([(500, 2)], Now);

		Assert.True(result.Succeeded);
		Assert.Equal(2, guild.Roles.Single(r => r.Id == 500).Position);
	}

	[Fact]
	public void DuplicateId_Returns_RoleReorderDuplicateId()
	{
		var guild = MakeGuild();

		var result = guild.ReorderRoles([(500, 2), (500, 3)], Now);

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.RoleReorderDuplicateId", result.Error.Code);
	}

	[Fact]
	public void UnknownId_Returns_RoleNotFound()
	{
		var guild = MakeGuild();

		var result = guild.ReorderRoles([(500, 3), (999, 2)], Now);

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.RoleNotFound", result.Error.Code);
	}

	[Fact]
	public void IncludesDefaultRole_Returns_RoleReorderIncludesDefault()
	{
		var guild = MakeGuild();

		// @everyone (101) is pinned and must not appear in the body
		var result = guild.ReorderRoles([(101, 2), (500, 0)], Now);

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.RoleReorderIncludesDefault", result.Error.Code);
	}

	[Fact]
	public void DuplicateTargetPositions_Returns_RoleReorderInvalidPositions()
	{
		var guild = MakeGuild();

		var result = guild.ReorderRoles([(500, 3), (501, 3)], Now);

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.RoleReorderInvalidPositions", result.Error.Code);
	}

	[Fact]
	public void TargetsNotMatchingOccupiedSlots_Returns_RoleReorderInvalidPositions()
	{
		var guild = MakeGuild();

		// A(2) and B(3) occupy {2,3}; targeting {3,4} is not a permutation of that
		var result = guild.ReorderRoles([(500, 3), (501, 4)], Now);

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.RoleReorderInvalidPositions", result.Error.Code);
	}

	[Fact]
	public void MoveIntoSlotHeldByRoleNotInBody_Returns_RoleReorderInvalidPositions()
	{
		var guild = MakeGuild();

		// A alone occupies {2}; moving it to 1 (admin's slot, not in the body)
		// would collide -> rejected
		var result = guild.ReorderRoles([(500, 1)], Now);

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.RoleReorderInvalidPositions", result.Error.Code);
	}

	[Fact]
	public void MoveIntoEveryoneSlot_Returns_RoleReorderInvalidPositions()
	{
		var guild = MakeGuild();

		// position 0 belongs to @everyone; no non-default role occupies it
		var result = guild.ReorderRoles([(500, 0)], Now);

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.RoleReorderInvalidPositions", result.Error.Code);
	}

	[Fact]
	public void Success_AdvancesUpdatedAt_OnGuildAndMovedRoles()
	{
		var guild = MakeGuild();
		var later = Now.AddMinutes(5);

		var result = guild.ReorderRoles([(500, 3), (501, 2)], later);

		Assert.True(result.Succeeded);
		Assert.Equal(later, guild.UpdatedAt);
		Assert.Equal(later, guild.Roles.Single(r => r.Id == 501).UpdatedAt);
	}
}
