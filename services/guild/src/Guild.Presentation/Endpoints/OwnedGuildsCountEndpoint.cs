using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Guilds.CountOwnedGuilds;
using Guild.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Guild.Presentation.Endpoints;

/// <summary>
/// internal owned-guilds-count lookup used by the Auth Service to enforce the
/// "transfer ownership before deleting account" rule on <c>DELETE /auth/me</c>.
/// not a Carter module so it stays out of the public <c>/v1</c> group; mounted
/// under <c>/internal</c> by <c>Program.cs</c>. the API Gateway only forwards
/// <c>/api/{service}/vN/...</c>, so <c>/internal/...</c> is unreachable from
/// outside the docker network
/// </summary>
public static class OwnedGuildsCountEndpoint
{
	public static void MapInternalRoutes(IEndpointRouteBuilder endpoints)
	{
		endpoints.MapGet("/users/{userId:long}/owned-guilds-count", GetAsync);
	}

	private static async Task<Results<
		Ok<OwnedGuildsCountResponse>,
		BadRequest<ErrorBody>>>
	GetAsync(
		long userId,
		IQueryHandler<CountOwnedGuildsQuery, Result<OwnedGuildsCountResponse>> handler,
		CancellationToken cancellationToken)
	{
		if (userId <= 0)
			return TypedResults.BadRequest(new ErrorBody("user_id must be a positive snowflake."));

		var result = await handler.HandleAsync(new CountOwnedGuildsQuery(userId), cancellationToken);

		// the handler treats "no guilds" and "unknown user" the same per contract,
		// so a success path is the only outcome from a well-formed request
		return TypedResults.Ok(result.Value);
	}
}
