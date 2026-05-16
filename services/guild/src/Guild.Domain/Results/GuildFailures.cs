namespace Guild.Domain.Results;

public static class GuildFailures
{
	public static readonly Failure GuildNotFound =
		new("Guild.GuildNotFound", "Guild not found.");

	public static readonly Failure GuildNameRequired =
		new("Guild.GuildNameRequired", "Guild name is required.");

	public static readonly Failure GuildNameTooLong =
		new("Guild.GuildNameTooLong", "Guild name must be 100 characters or fewer.");

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
}
