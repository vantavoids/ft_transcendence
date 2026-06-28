using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Guild.Presentation.Endpoints;

/// <summary>
/// OpenAPI response metadata for the guild endpoints. collapsing the per-endpoint
/// typed-results unions into <c>JsonHttpResult&lt;ErrorBody&gt;</c> (so the error
/// status now comes from the central <see cref="FailureStatus"/> map) removed the
/// error responses the generated OpenAPI / Scalar doc used to infer from those
/// unions. these helpers re-declare them so the API reference still lists the
/// 4xx responses every guild route can return as an <see cref="ErrorBody"/>.
/// </summary>
internal static class EndpointConventions
{
	/// <summary>the 4xx responses common to authenticated guild routes.</summary>
	public static RouteHandlerBuilder ProducesGuildErrors(this RouteHandlerBuilder builder) =>
		builder
			.Produces<ErrorBody>(StatusCodes.Status400BadRequest)
			.Produces<ErrorBody>(StatusCodes.Status403Forbidden)
			.Produces<ErrorBody>(StatusCodes.Status404NotFound);

	/// <summary>adds the 409 response for routes that can conflict (ban, join).</summary>
	public static RouteHandlerBuilder ProducesConflict(this RouteHandlerBuilder builder) =>
		builder.Produces<ErrorBody>(StatusCodes.Status409Conflict);
}
