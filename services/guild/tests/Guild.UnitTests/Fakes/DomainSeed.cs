using System.Reflection;
using Guild.Domain.Guild;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Fakes;

/// <summary>
/// reflection helpers used solely by tests to seed domain aggregates into
/// shapes the public API does not yet expose (e.g. a second member, a custom
/// role assignment, an overridden OwnerId). lives in the test assembly so the
/// production domain never grows a backdoor mutator
/// </summary>
internal static class DomainSeed
{
	private const BindingFlags AnyInstance =
		BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	internal static void SetOwnerId(GuildEntity guild, long newOwnerId)
	{
		typeof(GuildEntity)
			.GetProperty(nameof(GuildEntity.OwnerId), AnyInstance)!
			.SetValue(guild, newOwnerId);
	}

	internal static GuildMember AddMember(GuildEntity guild, long userId, DateTimeOffset joinedAt)
	{
		var member = (GuildMember)Activator.CreateInstance(
			typeof(GuildMember),
			AnyInstance,
			binder: null,
			args: null,
			culture: null)!;

		typeof(GuildMember).GetProperty(nameof(GuildMember.GuildId), AnyInstance)!.SetValue(member, guild.Id);
		typeof(GuildMember).GetProperty(nameof(GuildMember.UserId), AnyInstance)!.SetValue(member, userId);
		typeof(GuildMember).GetProperty(nameof(GuildMember.JoinedAt), AnyInstance)!.SetValue(member, joinedAt);

		var membersField = typeof(GuildEntity).GetField("_members", AnyInstance)!;
		var members = (List<GuildMember>)membersField.GetValue(guild)!;
		members.Add(member);
		return member;
	}

	internal static Role AddCustomRole(
		GuildEntity guild,
		long roleId,
		string name,
		long permissions,
		int position,
		DateTimeOffset now)
	{
		var role = (Role)Activator.CreateInstance(
			typeof(Role),
			AnyInstance,
			binder: null,
			args: null,
			culture: null)!;

		typeof(Role).GetProperty(nameof(Role.Id), AnyInstance)!.SetValue(role, roleId);
		typeof(Role).GetProperty(nameof(Role.GuildId), AnyInstance)!.SetValue(role, guild.Id);
		typeof(Role).GetProperty(nameof(Role.Name), AnyInstance)!.SetValue(role, name);
		typeof(Role).GetProperty(nameof(Role.Permissions), AnyInstance)!.SetValue(role, permissions);
		typeof(Role).GetProperty(nameof(Role.Position), AnyInstance)!.SetValue(role, position);
		typeof(Role).GetProperty(nameof(Role.IsDefault), AnyInstance)!.SetValue(role, false);
		typeof(Role).GetProperty(nameof(Role.IsHoisted), AnyInstance)!.SetValue(role, false);
		typeof(Role).GetProperty(nameof(Role.IsMentionable), AnyInstance)!.SetValue(role, false);
		typeof(Role).GetProperty(nameof(Role.CreatedAt), AnyInstance)!.SetValue(role, now);
		typeof(Role).GetProperty(nameof(Role.UpdatedAt), AnyInstance)!.SetValue(role, now);

		var rolesField = typeof(GuildEntity).GetField("_roles", AnyInstance)!;
		var roles = (List<Role>)rolesField.GetValue(guild)!;
		roles.Add(role);
		return role;
	}

	internal static MemberRole AssignRole(GuildEntity guild, long userId, long roleId, DateTimeOffset now)
	{
		var memberRole = (MemberRole)Activator.CreateInstance(
			typeof(MemberRole),
			AnyInstance,
			binder: null,
			args: null,
			culture: null)!;

		typeof(MemberRole).GetProperty(nameof(MemberRole.GuildId), AnyInstance)!.SetValue(memberRole, guild.Id);
		typeof(MemberRole).GetProperty(nameof(MemberRole.UserId), AnyInstance)!.SetValue(memberRole, userId);
		typeof(MemberRole).GetProperty(nameof(MemberRole.RoleId), AnyInstance)!.SetValue(memberRole, roleId);
		typeof(MemberRole).GetProperty(nameof(MemberRole.AssignedAt), AnyInstance)!.SetValue(memberRole, now);

		var memberRolesField = typeof(GuildEntity).GetField("_memberRoles", AnyInstance)!;
		var memberRoles = (List<MemberRole>)memberRolesField.GetValue(guild)!;
		memberRoles.Add(memberRole);
		return memberRole;
	}
}
