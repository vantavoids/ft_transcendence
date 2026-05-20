using Guild.Domain.Guild;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Domain;

public sealed class PermissionResolverTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public void Owner_AlwaysResolvesToAdministrator_RegardlessOfRoles()
	{
		var guild = CreateGuild(ownerId: 42);

		var mask = PermissionResolver.Resolve(
			userId: 42,
			ownerId: 42,
			allGuildRoles: guild.Roles,
			userAssignments: guild.MemberRoles);

		Assert.Equal((long)Permission.Administrator, mask);
	}

	[Fact]
	public void NonOwner_NoAssignedRoles_GetsOnlyDefaultRoleMask()
	{
		var guild = CreateGuild(ownerId: 1);

		// user 2 is not a member, but the resolver itself only checks
		// assignments; @everyone applies unconditionally
		var mask = PermissionResolver.Resolve(
			userId: 2,
			ownerId: 1,
			allGuildRoles: guild.Roles,
			userAssignments: Array.Empty<MemberRole>());

		Assert.Equal(515L, mask);
	}

	[Fact]
	public void NonOwner_WithAssignedRole_OrsTogetherDefaultAndAssigned()
	{
		var guild = CreateGuild(ownerId: 1);
		long modPerms = (long)Permission.KickMembers | (long)Permission.BanMembers;
		var modRole = DomainSeed.AddCustomRole(
			guild, roleId: 100, name: "Moderator", permissions: modPerms, position: 2, now: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: modRole.Id, now: Now);

		var mask = PermissionResolver.Resolve(
			userId: 2,
			ownerId: 1,
			allGuildRoles: guild.Roles,
			userAssignments: guild.MemberRoles);

		Assert.Equal(515L | modPerms, mask);
		Assert.Equal(563L, mask);
	}

	[Fact]
	public void NonOwner_AdministratorRoleAssigned_MaskIncludesAdministratorBit()
	{
		var guild = CreateGuild(ownerId: 1);
		// the seeded admin role for the owner. assign it explicitly to user 2.
		var adminRole = guild.Roles.Single(r => r.Name == "Administrator");
		DomainSeed.AssignRole(guild, userId: 2, roleId: adminRole.Id, now: Now);

		var mask = PermissionResolver.Resolve(
			userId: 2,
			ownerId: 1,
			allGuildRoles: guild.Roles,
			userAssignments: guild.MemberRoles);

		Assert.NotEqual(0L, mask & (long)Permission.Administrator);
	}

	[Fact]
	public void HasPermission_AdministratorBit_GrantsEverything()
	{
		long mask = (long)Permission.Administrator;

		Assert.True(PermissionResolver.HasPermission(mask, Permission.ManageGuild));
		Assert.True(PermissionResolver.HasPermission(mask, Permission.KickMembers));
	}

	[Fact]
	public void HasPermission_ExactBit()
	{
		long mask = (long)Permission.ManageGuild;

		Assert.True(PermissionResolver.HasPermission(mask, Permission.ManageGuild));
		Assert.False(PermissionResolver.HasPermission(mask, Permission.KickMembers));
	}

	[Fact]
	public void HasPermission_MissingBit()
	{
		long mask = (long)Permission.SendMessages;

		Assert.False(PermissionResolver.HasPermission(mask, Permission.ManageGuild));
	}

	[Fact]
	public void NonOwner_TwoRolesGrantingSameBit_BitIsGrantedNotXored()
	{
		// if perms were accumulated with ^= instead of |=, a bit shared by two
		// assigned roles would cancel back to 0; this test catches that mutation
		var guild = CreateGuild(ownerId: 1);
		long sharedBit = (long)Permission.ManageGuild;
		var role1 = DomainSeed.AddCustomRole(
			guild, roleId: 100, name: "R1", permissions: sharedBit, position: 2, now: Now);
		var role2 = DomainSeed.AddCustomRole(
			guild, roleId: 101, name: "R2", permissions: sharedBit, position: 3, now: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: role1.Id, now: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: role2.Id, now: Now);

		var mask = PermissionResolver.Resolve(
			userId: 2,
			ownerId: 1,
			allGuildRoles: guild.Roles,
			userAssignments: guild.MemberRoles);

		Assert.NotEqual(0L, mask & sharedBit);
	}

	[Fact]
	public void Resolver_IgnoresAssignmentsForOtherUsers()
	{
		var guild = CreateGuild(ownerId: 1);
		long modPerms = (long)Permission.KickMembers;
		var modRole = DomainSeed.AddCustomRole(
			guild, roleId: 100, name: "Moderator", permissions: modPerms, position: 2, now: Now);
		// assignment belongs to user 99, not 42; should be ignored
		DomainSeed.AssignRole(guild, userId: 99, roleId: modRole.Id, now: Now);

		var mask = PermissionResolver.Resolve(
			userId: 42,
			ownerId: 1,
			allGuildRoles: guild.Roles,
			userAssignments: guild.MemberRoles);

		Assert.Equal(515L, mask);
	}

	private static GuildEntity CreateGuild(long ownerId) =>
		GuildEntity.Create(
			id: 10,
			name: "Test",
			description: null,
			iconUrl: null,
			bannerUrl: null,
			ownerId: ownerId,
			everyoneRoleId: 11,
			adminRoleId: 12,
			now: Now).Value;
}
