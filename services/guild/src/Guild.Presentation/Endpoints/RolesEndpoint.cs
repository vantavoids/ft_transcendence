using Carter;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Roles.Common;
using Guild.Application.Features.Roles.CreateRole;
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
		group.MapPost("/", CreateAsync);
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

	private static async Task<Results<
		Created<RoleResponse>,
		BadRequest<ErrorBody>,
		NotFound<ErrorBody>,
		JsonHttpResult<ErrorBody>>>
	CreateAsync(
		long id,
		CreateRoleRequest request,
		ICommandHandler<CreateRoleCommand, Result<RoleResponse>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new CreateRoleCommand(
				GuildId: id,
				Name: request.Name,
				Color: request.Color,
				Permissions: request.Permissions ?? 0L,
				IsHoisted: request.IsHoisted ?? false,
				IsMentionable: request.IsMentionable ?? false),
			cancellationToken);

		return result.Succeeded
			? TypedResults.Created($"/v1/guilds/{id}/roles/{result.Value.Id}", result.Value)
			: MapErrorCreate(result.Error);
	}

	// ---- error mapping ----

	private static Results<Ok<IReadOnlyList<RoleResponse>>, NotFound<ErrorBody>, JsonHttpResult<ErrorBody>>
		MapErrorList(Failure failure) => failure.Code switch
		{
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			_ => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
		};

	private static Results<Created<RoleResponse>, BadRequest<ErrorBody>, NotFound<ErrorBody>, JsonHttpResult<ErrorBody>>
		MapErrorCreate(Failure failure) => failure.Code switch
		{
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			"Guild.NotAMember" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.MissingPermission" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.CannotGrantPermissionsYouLack" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			_ => TypedResults.BadRequest(new ErrorBody(failure.Message)),
		};

	// ---- request shapes ----

	private sealed record CreateRoleRequest(
		string? Name,
		string? Color,
		long? Permissions,
		bool? IsHoisted,
		bool? IsMentionable);
}
