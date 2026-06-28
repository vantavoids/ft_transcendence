using Guild.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Guild.Presentation.Endpoints;

/// <summary>
/// single source of truth mapping a domain <see cref="Failure"/> code to its HTTP
/// status, derived from <c>docs/contracts/guild.md</c>. replaces the per-endpoint
/// switch arms that previously drifted (e.g. Leave fell back to 403 instead of
/// 400). codes absent from the table are request/validation problems and map to
/// <see cref="StatusCodes.Status400BadRequest"/>.
/// </summary>
/// <remarks>
/// a handful of codes are status-ambiguous because the contract documents them
/// differently per endpoint (the clearest case: an invalid invite is 400 on
/// <c>POST /guilds/{id}/join</c> but 404 on <c>POST /invites/{code}/join</c>).
/// those endpoints pass per-call overrides to <see cref="EndpointResults.Problem"/>;
/// the table holds the more common status as the default.
/// </remarks>
internal static class FailureStatus
{
	private static readonly Dictionary<string, int> Map = new(StringComparer.Ordinal)
	{
		// 404 Not Found
		["Guild.GuildNotFound"] = StatusCodes.Status404NotFound,
		["Guild.CategoryNotFound"] = StatusCodes.Status404NotFound,
		["Guild.ChannelNotFound"] = StatusCodes.Status404NotFound,
		["Guild.RoleNotFound"] = StatusCodes.Status404NotFound,
		["Guild.BanNotFound"] = StatusCodes.Status404NotFound,
		["Guild.OverwriteNotFound"] = StatusCodes.Status404NotFound,
		["Guild.RoleAssignmentNotFound"] = StatusCodes.Status404NotFound,
		["Guild.InviteNotFound"] = StatusCodes.Status404NotFound,
		["Guild.InviteAlreadyRevoked"] = StatusCodes.Status404NotFound,
		["Guild.InviteGuildMismatch"] = StatusCodes.Status404NotFound,
		["Guild.InviteUnusable"] = StatusCodes.Status404NotFound,
		["Guild.TargetNotAMember"] = StatusCodes.Status404NotFound,

		// 403 Forbidden
		["Guild.NotAMember"] = StatusCodes.Status403Forbidden,
		["Guild.MissingPermission"] = StatusCodes.Status403Forbidden,
		["Guild.RoleHierarchyBlocked"] = StatusCodes.Status403Forbidden,
		["Guild.CannotGrantPermissionsYouLack"] = StatusCodes.Status403Forbidden,
		["Guild.NotTheOwner"] = StatusCodes.Status403Forbidden,
		["Guild.JoinBannedFromGuild"] = StatusCodes.Status403Forbidden,
		// NOTE: CannotBanOwner is 403 but CannotKickOwner is 400 (per contract,
		// Ban vs Kick error tables); the latter falls through to the 400 default.
		["Guild.CannotBanOwner"] = StatusCodes.Status403Forbidden,

		// 409 Conflict
		["Guild.AlreadyBanned"] = StatusCodes.Status409Conflict,
		["Guild.AlreadyMember"] = StatusCodes.Status409Conflict,
	};

	public static int Of(Failure failure) =>
		Map.GetValueOrDefault(failure.Code, StatusCodes.Status400BadRequest);
}

/// <summary>
/// turns a <see cref="Failure"/> into the canonical error response. endpoints
/// return <c>Results&lt;TSuccess, JsonHttpResult&lt;ErrorBody&gt;&gt;</c> and call
/// <see cref="Problem"/> on the failure path.
/// </summary>
internal static class EndpointResults
{
	public static JsonHttpResult<ErrorBody> Problem(
		Failure failure,
		params (string Code, int Status)[] overrides)
	{
		foreach (var (code, status) in overrides)
		{
			if (failure.Code == code)
				return TypedResults.Json(new ErrorBody(failure.Message), statusCode: status);
		}

		return TypedResults.Json(new ErrorBody(failure.Message), statusCode: FailureStatus.Of(failure));
	}
}
