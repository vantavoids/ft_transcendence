using Carter;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Roles.Common;
using Guild.Application.Features.Roles.ListRoles;
using Guild.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Guild.Presentation.Endpoints;

public sealed class RolesEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup("/guilds/{id:long}/roles");
		group.MapGet("/", ListAsync);
	}

	private static async Task<Results<
		Ok<IReadOnlyList<RoleResponse>>,
		NotFound<ErrorBody>,
		JsonHttpResult<ErrorBody>>>
	ListAsync(
		long id,
		IQueryHandler<ListRolesQuery, Result<RoleListResponse>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(new ListRolesQuery(id), cancellationToken);
		return result.Succeeded
			? TypedResults.Ok(result.Value.Items)
			: MapErrorList(result.Error);
	}

	// ---- error mapping ----

	private static Results<Ok<IReadOnlyList<RoleResponse>>, NotFound<ErrorBody>, JsonHttpResult<ErrorBody>>
		MapErrorList(Failure failure) => failure.Code switch
		{
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			_ => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
		};
}
