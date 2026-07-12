using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Guilds.UserGuildIds;
using Guild.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Guild.Presentation.Endpoints;

/// <summary>
/// internal guild-membership lookup used by the Chat Service on hub connect to
/// join a connection to every <c>guild:{id}</c> SignalR group the user belongs
/// to. mounted under <c>/internal</c> by <c>Program.cs</c>; unreachable from the
/// gateway, which only forwards <c>/api/{service}/vN/...</c>. ids are serialized
/// as quoted strings to match the wire policy.
/// </summary>
public static class UserGuildsEndpoint
{
	public static void MapInternalRoutes(IEndpointRouteBuilder endpoints)
	{
		endpoints.MapGet("/users/{userId:long}/guilds", GetAsync);
	}

	private static async Task<Results<
		Ok<IReadOnlyList<string>>,
		BadRequest<ErrorBody>>>
	GetAsync(
		long userId,
		IQueryHandler<GetUserGuildIdsQuery, Result<IReadOnlyList<long>>> handler,
		CancellationToken cancellationToken)
	{
		if (userId <= 0)
			return TypedResults.BadRequest(new ErrorBody("user_id must be a positive snowflake."));

		var result = await handler.HandleAsync(new GetUserGuildIdsQuery(userId), cancellationToken);

		// guild-less/unknown users resolve to an empty list upstream, so a
		// well-formed request always succeeds - same shape as visible-channels
		var ids = result.Value.Select(id => id.ToString()).ToList();
		return TypedResults.Ok<IReadOnlyList<string>>(ids);
	}
}
