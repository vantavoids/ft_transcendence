// TODO: gate this behind an internal service token (shared secret header)?
using Carter;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Channels.Membership;
using Guild.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Guild.Presentation.Endpoints;

public sealed class ChannelMembershipEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder endpoints)
	{
		// the parent group at /v1 has .RequireAuthorization() applied; override
		// here so the Chat Service can call this without a user JWT
		endpoints.MapGet("/channels/{channelId:long}/membership", GetAsync)
			.AllowAnonymous();
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
