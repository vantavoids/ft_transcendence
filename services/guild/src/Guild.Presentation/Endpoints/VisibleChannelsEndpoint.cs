using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Channels.Common;
using Guild.Application.Features.Channels.ListChannels;
using Guild.Application.Features.Channels.VisibleChannels;
using Guild.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Guild.Presentation.Endpoints;

/// <summary>
/// internal visible-channels lookup used by the Chat Service to back
/// <c>GET /channels/read-states</c> without an N+1 chain over guilds/channels.
/// not a Carter module so it stays out of the public <c>/v1</c> group; mounted
/// under <c>/internal</c> by <c>Program.cs</c>. the API Gateway only forwards
/// <c>/api/{service}/vN/...</c>, so <c>/internal/...</c> is unreachable from
/// outside the docker network
/// </summary>
public static class VisibleChannelsEndpoint
{
	public static void MapInternalRoutes(IEndpointRouteBuilder endpoints)
	{
		endpoints.MapGet("/users/{userId:long}/channels", GetAsync);
	}

	private static async Task<Results<
		Ok<IReadOnlyList<ChannelResponse>>,
		BadRequest<ErrorBody>>>
	GetAsync(
		long userId,
		IQueryHandler<GetVisibleChannelsQuery, Result<ChannelListResponse>> handler,
		CancellationToken cancellationToken)
	{
		if (userId <= 0)
			return TypedResults.BadRequest(new ErrorBody("user_id must be a positive snowflake."));

		var result = await handler.HandleAsync(new GetVisibleChannelsQuery(userId), cancellationToken);

		// guild-less/unknown users resolve to an empty guild-id list upstream, so
		// a well-formed request always succeeds - same shape as owned-guilds-count
		return TypedResults.Ok(result.Value.Items);
	}
}
