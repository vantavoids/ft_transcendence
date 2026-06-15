using Carter;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Membership.GetMemberPermissions;
using Guild.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Guild.Presentation.Endpoints;

public sealed class MemberPermissionsEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder endpoints)
	{
		endpoints.MapGet(
			"/guilds/{id:long}/members/{userId:long}/permissions",
			GetAsync);
	}

	private static async Task<Results<
		Ok<MemberPermissionsResponse>,
		BadRequest<ErrorBody>,
		NotFound<ErrorBody>,
		JsonHttpResult<ErrorBody>>>
	GetAsync(
		long id,
		long userId,
		IQueryHandler<GetMemberPermissionsQuery, Result<MemberPermissionsResponse>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new GetMemberPermissionsQuery(id, userId), cancellationToken);

		if (result.Succeeded)
			return TypedResults.Ok(result.Value);

		return result.Error.Code switch
		{
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(result.Error.Message)),
			"Guild.TargetNotAMember" => TypedResults.NotFound(new ErrorBody(result.Error.Message)),
			"Guild.NotAMember" => TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status403Forbidden),
			_ => TypedResults.BadRequest(new ErrorBody(result.Error.Message)),
		};
	}
}
