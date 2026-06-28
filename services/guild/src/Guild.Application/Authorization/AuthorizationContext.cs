using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.Application.Authorization;

/// <summary>
/// the result of the authorization preamble that almost every guild-scoped
/// handler runs: load the guild with its membership graph, confirm the caller
/// is a member, and (optionally) confirm the caller holds a required
/// permission. carries the loaded <see cref="Guild"/> and the caller's
/// <see cref="EffectiveMask"/> so the handler can do any further checks
/// (hierarchy, grant, self-vs-other) without re-loading or re-resolving.
/// </summary>
internal sealed class AuthorizationContext
{
	private AuthorizationContext(GuildEntity guild, long effectiveMask)
	{
		Guild = guild;
		EffectiveMask = effectiveMask;
	}

	public GuildEntity Guild { get; }
	public long EffectiveMask { get; }

	/// <summary>
	/// loads the guild and runs the standard preamble. returns
	/// <see cref="GuildFailures.GuildNotFound"/> when the guild does not exist,
	/// <see cref="GuildFailures.NotAMember"/> when the caller is not a member,
	/// and <see cref="GuildFailures.MissingPermission"/> when
	/// <paramref name="required"/> is set and the caller lacks it. pass
	/// <see cref="Permission.None"/> for a membership-only gate (reads, or
	/// handlers whose permission requirement is conditional).
	/// </summary>
	public static async Task<Result<AuthorizationContext>> LoadAsync(
		IGuildRepository guilds,
		ICurrentUser currentUser,
		long guildId,
		Permission required,
		CancellationToken cancellationToken = default)
	{
		var guild = await guilds.GetByIdWithMembershipAsync(guildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		if (guild.Members.All(m => m.UserId != currentUser.Id))
			return GuildFailures.NotAMember;

		var mask = PermissionResolver.Resolve(guild, currentUser.Id);
		if (required != Permission.None && !PermissionResolver.HasPermission(mask, required))
			return GuildFailures.MissingPermission;

		return new AuthorizationContext(guild, mask);
	}
}
