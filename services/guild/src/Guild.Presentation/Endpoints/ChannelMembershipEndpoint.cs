using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Channels.Membership;
using Guild.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Guild.Presentation.Endpoints;

/// <summary>
/// internal channel-membership lookup used by the Chat Service.
/// not a Carter module so it stays out of the public <c>/v1</c> group;
/// mounted under <c>/internal</c> by <c>Program.cs</c>. the API Gateway only
/// forwards <c>/api/{service}/vN/...</c>, so <c>/internal/...</c> is
/// unreachable from outside the docker network
/// </summary>
public static class ChannelMembershipEndpoint
{
	public static void MapInternalRoutes(IEndpointRouteBuilder endpoints)
	{
		endpoints.MapGet("/channels/{channelId:long}/membership", GetAsync);
	}

	private static async Task<Results<
		Ok<MembershipResponse>,
		BadRequest<ErrorBody>,
		NotFound<ErrorBody>>>
	GetAsync(
		long channelId,
		string? user_id,
		IQueryHandler<GetChannelMembershipQuery, Result<MembershipResponse>> handler,
		CancellationToken cancellationToken)
	{
		// user_id is a snowflake (long). the contract doc lists it as uuid but
		// snowflakes are the project-wide id format -> parse as long
		if (string.IsNullOrWhiteSpace(user_id) || !long.TryParse(user_id, out var userId) || userId <= 0)
			return TypedResults.BadRequest(new ErrorBody("user_id query parameter must be a positive snowflake."));

		var result = await handler.HandleAsync(
			new GetChannelMembershipQuery(channelId, userId),
			cancellationToken);

		if (result.Succeeded)
			return TypedResults.Ok(result.Value);

		return result.Error.Code switch
		{
			"Guild.ChannelNotFound" => TypedResults.NotFound(new ErrorBody(result.Error.Message)),
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(result.Error.Message)),
			_ => TypedResults.BadRequest(new ErrorBody(result.Error.Message)),
		};
	}
}
