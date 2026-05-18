using Guild.Domain.Guild;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Domain;

/// <summary>
/// covers the channel-aware <c>PermissionResolver.Resolve(Guild, long, Channel, IReadOnlyList&lt;ChannelPermissionOverwrite&gt;)</c>
/// overload introduced for the channel-permissions bundle. precedence:
/// owner -> all bits; non-member -> 0; role-aggregate -> @everyone-overwrite ->
/// role-overwrites -> member-overwrite; Administrator short-circuits
/// </summary>
public sealed class ChannelPermissionResolverTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public void Owner_GetsAllBits_RegardlessOfRoles()
	{
		var guild = CreateGuild(ownerId: 42);
		var channel = MakeChannel(guild.Id);

		var mask = PermissionResolver.Resolve(
			guild, userId: 42, channel, overwrites: Array.Empty<ChannelPermissionOverwrite>());

		Assert.Equal(~0L, mask);
	}

	[Fact]
	public void NonMember_ReturnsZero()
	{
		var guild = CreateGuild(ownerId: 1);
		var channel = MakeChannel(guild.Id);

		var mask = PermissionResolver.Resolve(
			guild, userId: 999, channel, overwrites: Array.Empty<ChannelPermissionOverwrite>());

		Assert.Equal(0L, mask);
	}

	[Fact]
	public void Member_OnlyEveryoneRole_GetsEveryoneMask()
	{
		var guild = CreateGuild(ownerId: 1);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		var channel = MakeChannel(guild.Id);

		var mask = PermissionResolver.Resolve(
			guild, userId: 2, channel, overwrites: Array.Empty<ChannelPermissionOverwrite>());

		// @everyone seeded with 1|2|512 = 515
		Assert.Equal(515L, mask);
	}

	[Fact]
	public void Member_WithAdministratorRole_GetsAllBits()
	{
		var guild = CreateGuild(ownerId: 1);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		var admin = guild.Roles.Single(r => r.Name == "Administrator");
		DomainSeed.AssignRole(guild, userId: 2, roleId: admin.Id, now: Now);
		var channel = MakeChannel(guild.Id);

		// even with deny-all overwrites, Administrator short-circuits before
		// overwrites are applied
		var denyAll = ChannelPermissionOverwrite.Create(
			id: 9001, channelId: channel.Id,
			targetType: OverwriteTargetType.Member, targetId: 2,
			allow: 0, deny: 0xFFFF, now: Now).Value;

		var mask = PermissionResolver.Resolve(
			guild, userId: 2, channel,
			overwrites: new[] { denyAll });

		Assert.Equal(~0L, mask);
	}

	[Fact]
	public void EveryoneOverwrite_AppliesAfterRoleAggregate()
	{
		var guild = CreateGuild(ownerId: 1);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		var everyone = guild.Roles.Single(r => r.IsDefault);
		var channel = MakeChannel(guild.Id);

		// @everyone base perms = 515 (SendMessages|ReadMessages|CreateInvite)
		// deny SendMessages (1), allow ManageMessages (4)
		var ow = ChannelPermissionOverwrite.Create(
			id: 1, channelId: channel.Id,
			targetType: OverwriteTargetType.Role, targetId: everyone.Id,
			allow: (long)Permission.ManageMessages,
			deny: (long)Permission.SendMessages,
			now: Now).Value;

		var mask = PermissionResolver.Resolve(
			guild, userId: 2, channel,
			overwrites: new[] { ow });

		// 515 -> (~1 mask off) 514 -> | 4 = 518
		Assert.Equal(514L | (long)Permission.ManageMessages, mask);
	}

	[Fact]
	public void RoleOverwrites_AggregateAllowsAndDenies()
	{
		var guild = CreateGuild(ownerId: 1);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);

		var role1 = DomainSeed.AddCustomRole(guild, roleId: 200, name: "R1",
			permissions: 0L, position: 1, now: Now);
		var role2 = DomainSeed.AddCustomRole(guild, roleId: 201, name: "R2",
			permissions: 0L, position: 2, now: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: role1.Id, now: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: role2.Id, now: Now);

		var channel = MakeChannel(guild.Id);

		// role1 allows ManageMessages(4); role2 denies ReadMessages(2) and allows BanMembers(32)
		var ow1 = ChannelPermissionOverwrite.Create(
			id: 1, channelId: channel.Id,
			targetType: OverwriteTargetType.Role, targetId: role1.Id,
			allow: (long)Permission.ManageMessages, deny: 0L, now: Now).Value;
		var ow2 = ChannelPermissionOverwrite.Create(
			id: 2, channelId: channel.Id,
			targetType: OverwriteTargetType.Role, targetId: role2.Id,
			allow: (long)Permission.BanMembers,
			deny: (long)Permission.ReadMessages, now: Now).Value;

		var mask = PermissionResolver.Resolve(
			guild, userId: 2, channel,
			overwrites: new[] { ow1, ow2 });

		// base = 515 (1|2|512). aggDeny = 2, aggAllow = 4 | 32 = 36
		// (515 & ~2) | 36 = 513 | 36 = 549
		Assert.Equal(549L, mask);
	}

	[Fact]
	public void MemberOverwrite_OverridesRoleAggregate()
	{
		var guild = CreateGuild(ownerId: 1);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);

		var role = DomainSeed.AddCustomRole(guild, roleId: 200, name: "Mod",
			permissions: 0L, position: 1, now: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: role.Id, now: Now);

		var channel = MakeChannel(guild.Id);

		// role overwrite denies SendMessages
		var roleOw = ChannelPermissionOverwrite.Create(
			id: 1, channelId: channel.Id,
			targetType: OverwriteTargetType.Role, targetId: role.Id,
			allow: 0L, deny: (long)Permission.SendMessages, now: Now).Value;
		// member overwrite re-allows SendMessages (winning)
		var memberOw = ChannelPermissionOverwrite.Create(
			id: 2, channelId: channel.Id,
			targetType: OverwriteTargetType.Member, targetId: 2,
			allow: (long)Permission.SendMessages, deny: 0L, now: Now).Value;

		var mask = PermissionResolver.Resolve(
			guild, userId: 2, channel,
			overwrites: new[] { roleOw, memberOw });

		Assert.NotEqual(0L, mask & (long)Permission.SendMessages);
	}

	[Fact]
	public void Precedence_RoleAggregate_LessThan_MemberOverwrite()
	{
		var guild = CreateGuild(ownerId: 1);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);

		var channel = MakeChannel(guild.Id);

		// member overwrite denies a bit @everyone normally grants
		var memberOw = ChannelPermissionOverwrite.Create(
			id: 1, channelId: channel.Id,
			targetType: OverwriteTargetType.Member, targetId: 2,
			allow: 0L, deny: (long)Permission.ReadMessages, now: Now).Value;

		var mask = PermissionResolver.Resolve(
			guild, userId: 2, channel,
			overwrites: new[] { memberOw });

		// @everyone grants ReadMessages(2); member overwrite denies it
		Assert.Equal(0L, mask & (long)Permission.ReadMessages);
	}

	[Fact]
	public void OverwritesForOtherRoles_AreIgnored()
	{
		var guild = CreateGuild(ownerId: 1);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);

		// add a role but DO NOT assign it to the user
		var role = DomainSeed.AddCustomRole(guild, roleId: 200, name: "Unassigned",
			permissions: 0L, position: 1, now: Now);

		var channel = MakeChannel(guild.Id);
		var ow = ChannelPermissionOverwrite.Create(
			id: 1, channelId: channel.Id,
			targetType: OverwriteTargetType.Role, targetId: role.Id,
			allow: 0L, deny: (long)Permission.ReadMessages, now: Now).Value;

		var mask = PermissionResolver.Resolve(
			guild, userId: 2, channel, overwrites: new[] { ow });

		// the unassigned role's deny must NOT apply -> ReadMessages still granted
		Assert.NotEqual(0L, mask & (long)Permission.ReadMessages);
	}

	private static GuildEntity CreateGuild(long ownerId) =>
		GuildEntity.Create(
			id: 10, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: ownerId, everyoneRoleId: 11, adminRoleId: 12, now: Now).Value;

	private static Channel MakeChannel(long guildId) =>
		Channel.Create(
			id: 5000, guildId: guildId, categoryId: null,
			name: "general", topic: null, type: ChannelType.Text,
			position: 0, now: Now).Value;
}
