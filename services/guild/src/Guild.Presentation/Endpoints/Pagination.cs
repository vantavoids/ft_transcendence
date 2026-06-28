using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Guild.Presentation.Endpoints;

/// <summary>
/// shared keyset-pagination query parsing for the list endpoints (members, bans,
/// ...). validates the <c>after</c> cursor and clamps/checks <c>limit</c> so the
/// rules and the default/max limits live in one place instead of being copied
/// into every paginated endpoint.
/// </summary>
internal static class Pagination
{
	public const int DefaultLimit = 50;
	public const int MaxLimit = 100;

	/// <summary>
	/// parses <paramref name="after"/> / <paramref name="limit"/>. on success
	/// <c>Error</c> is null and the cursor + effective limit are set; on a bad
	/// input <c>Error</c> is a 400 describing the problem and the other values
	/// should be ignored.
	/// </summary>
	public static (long? After, int Limit, JsonHttpResult<ErrorBody>? Error) Parse(string? after, int? limit)
	{
		long? afterCursor = null;
		if (after is not null)
		{
			if (!long.TryParse(after, out var parsed) || parsed <= 0)
				return (null, 0, TypedResults.Json(
					new ErrorBody("after must be a positive snowflake."),
					statusCode: StatusCodes.Status400BadRequest));
			afterCursor = parsed;
		}

		var effectiveLimit = limit ?? DefaultLimit;
		if (effectiveLimit <= 0 || effectiveLimit > MaxLimit)
			return (null, 0, TypedResults.Json(
				new ErrorBody($"limit must be between 1 and {MaxLimit}."),
				statusCode: StatusCodes.Status400BadRequest));

		return (afterCursor, effectiveLimit, null);
	}
}
