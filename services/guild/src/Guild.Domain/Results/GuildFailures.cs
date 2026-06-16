namespace Guild.Domain.Results;

public static class GuildFailures
{
	public static readonly Failure GuildNotFound =
		new("Guild.GuildNotFound", "Guild not found.");

	public static readonly Failure GuildNameRequired =
		new("Guild.GuildNameRequired", "Guild name is required.");

	public static readonly Failure GuildNameTooLong =
		new("Guild.GuildNameTooLong", "Guild name must be 100 characters or fewer.");

	public static readonly Failure GuildNameInvalid =
		new("Guild.GuildNameInvalid", "Guild name must not contain control characters.");

	public static readonly Failure GuildDescriptionTooLong =
		new("Guild.GuildDescriptionTooLong", "Guild description is too long.");

	public static readonly Failure GuildIconUrlTooLong =
		new("Guild.GuildIconUrlTooLong", "Guild icon URL must be 512 characters or fewer.");

	public static readonly Failure GuildBannerUrlTooLong =
		new("Guild.GuildBannerUrlTooLong", "Guild banner URL must be 512 characters or fewer.");

	public static readonly Failure NotAMember =
		new("Guild.NotAMember", "Caller is not a member of this guild.");

	public static readonly Failure NotTheOwner =
		new("Guild.NotTheOwner", "Only the owner can perform this action.");

	public static readonly Failure MissingPermission =
		new("Guild.MissingPermission", "Caller is missing the required permission.");

	public static readonly Failure TargetNotAMember =
		new("Guild.TargetNotAMember", "Target user is not a member of this guild.");

	public static readonly Failure InvalidRoleName =
		new("Guild.InvalidRoleName", "Role name is invalid.");

	public static readonly Failure InvalidColor =
		new("Guild.InvalidColor", "Color must be a hex string of the form '#RRGGBB'.");

	public static readonly Failure CategoryNameRequired =
		new("Guild.CategoryNameRequired", "Category name is required.");

	public static readonly Failure CategoryNameTooLong =
		new("Guild.CategoryNameTooLong", "Category name must be 100 characters or fewer.");

	public static readonly Failure CategoryNameInvalid =
		new("Guild.CategoryNameInvalid", "Category name must not contain control characters.");

	public static readonly Failure CategoryNotFound =
		new("Guild.CategoryNotFound", "Channel category not found.");

	public static readonly Failure CategoryFromDifferentGuild =
		new("Guild.CategoryFromDifferentGuild", "Channel category belongs to a different guild.");

	public static readonly Failure ChannelNameRequired =
		new("Guild.ChannelNameRequired", "Channel name is required.");

	public static readonly Failure ChannelNameTooLong =
		new("Guild.ChannelNameTooLong", "Channel name must be 100 characters or fewer.");

	public static readonly Failure ChannelNameInvalid =
		new("Guild.ChannelNameInvalid", "Channel name may only contain lowercase letters, digits, hyphens and underscores.");

	public static readonly Failure ChannelTopicTooLong =
		new("Guild.ChannelTopicTooLong", "Channel topic must be 1024 characters or fewer.");

	public static readonly Failure ChannelInvalidType =
		new("Guild.ChannelInvalidType", "Channel type is invalid.");

	public static readonly Failure ChannelNotFound =
		new("Guild.ChannelNotFound", "Channel not found.");

	public static readonly Failure OverwriteInvalidTarget =
		new("Guild.OverwriteInvalidTarget", "Overwrite target_id does not reference a real role or member in the guild.");

	public static readonly Failure OverwriteAllowDenyOverlap =
		new("Guild.OverwriteAllowDenyOverlap", "A bit cannot be both allowed and denied by the same overwrite.");

	public static readonly Failure OverwriteNotFound =
		new("Guild.OverwriteNotFound", "Channel permission overwrite not found.");

	public static readonly Failure AlreadyMember =
		new("Guild.AlreadyMember", "User is already a member of this guild.");

	public static readonly Failure OwnerCannotLeave =
		new("Guild.OwnerCannotLeave", "Owner cannot leave. Transfer ownership or delete the guild first.");

	public static readonly Failure CannotKickOwner =
		new("Guild.CannotKickOwner", "The owner cannot be kicked.");

	public static readonly Failure RoleNotFound =
		new("Guild.RoleNotFound", "Role not found.");

	public static readonly Failure CannotEditDefaultRole =
		new("Guild.CannotEditDefaultRole", "The default role's name, position and is_default flag cannot be changed.");

	public static readonly Failure CannotDeleteDefaultRole =
		new("Guild.CannotDeleteDefaultRole", "The default role cannot be deleted.");

	public static readonly Failure RoleHierarchyBlocked =
		new("Guild.RoleHierarchyBlocked", "Caller does not out-rank target in the role hierarchy.");

	public static readonly Failure NicknameTooLong =
		new("Guild.NicknameTooLong", "Member nickname must be 64 characters or fewer.");

	public static readonly Failure NicknameInvalid =
		new("Guild.NicknameInvalid", "Member nickname contains invalid characters");

	public static readonly Failure CannotGrantPermissionsYouLack =
		new("Guild.CannotGrantPermissionsYouLack", "Cannot grant permissions the caller does not have.");

	public static readonly Failure RolePermissionsInvalid =
		new("Guild.RolePermissionsInvalid", "Role permissions bitmask must be non-negative.");

	public static readonly Failure RolePositionInvalid =
		new("Guild.RolePositionInvalid", "Role position must be non-negative.");

	public static readonly Failure InviteCodeRequired =
		new("Guild.InviteCodeRequired", "Invite code is required.");

	public static readonly Failure InviteCodeInvalid =
		new("Guild.InviteCodeInvalid", "Invite code is invalid.");

	public static readonly Failure InviteMaxUsesInvalid =
		new("Guild.InviteMaxUsesInvalid", "Invite max_uses must be a positive integer.");

	public static readonly Failure InviteExpiresInPast =
		new("Guild.InviteExpiresInPast", "Invite expires_at must be in the future.");

	public static readonly Failure InviteNotFound =
		new("Guild.InviteNotFound", "Invite not found.");

	public static readonly Failure InviteUnusable =
		new("Guild.InviteUnusable", "Invite is revoked, expired, or has reached its max uses.");

	public static readonly Failure InviteAlreadyRevoked =
		new("Guild.InviteAlreadyRevoked", "Invite is already revoked.");

	public static readonly Failure InviteGuildMismatch =
		new("Guild.InviteGuildMismatch", "Invite code does not belong to this guild.");

	public static readonly Failure MaxRolesReached =
		new ("Guild.MaxRolesReached", "Guild has reached the maximum number of roles");

	public static readonly Failure BanReasonTooLong =
		new("Guild.BanReasonTooLong", "Ban reason must be 512 characters or fewer.");
}
