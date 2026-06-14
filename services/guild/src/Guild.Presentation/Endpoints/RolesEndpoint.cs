using Carter;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Roles.Common;
using Guild.Application.Features.Roles.CreateRole;
using Guild.Application.Features.Roles.DeleteRole;
using Guild.Application.Features.Roles.ListRoles;
using Guild.Application.Features.Roles.UpdateRole;
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
		group.MapPatch("/{roleId:long}", UpdateAsync);
		group.MapDelete("/{roleId:long}", DeleteAsync);
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

	private static async Task<Results<
		Ok<RoleResponse>,
		BadRequest<ErrorBody>,
		NotFound<ErrorBody>,
		JsonHttpResult<ErrorBody>>>
	UpdateAsync(
		long id,
		long roleId,
		UpdateRoleRequest request,
		ICommandHandler<UpdateRoleCommand, Result<RoleResponse>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new UpdateRoleCommand(
				GuildId: id,
				RoleId: roleId,
				Name: request.Name,
				Color: request.Color,
				Permissions: request.Permissions,
				Position: request.Position,
				IsHoisted: request.IsHoisted,
				IsMentionable: request.IsMentionable),
			cancellationToken);

		return result.Succeeded
			? TypedResults.Ok(result.Value)
			: MapErrorUpdate(result.Error);
	}

	private static async Task<Results<
		NoContent,
		BadRequest<ErrorBody>,
		NotFound<ErrorBody>,
		JsonHttpResult<ErrorBody>>>
	DeleteAsync(
		long id,
		long roleId,
		ICommandHandler<DeleteRoleCommand, Result> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(new DeleteRoleCommand(id, roleId), cancellationToken);
		return result.Succeeded
			? TypedResults.NoContent()
			: MapErrorDelete(result.Error);
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

	private static Results<Ok<RoleResponse>, BadRequest<ErrorBody>, NotFound<ErrorBody>, JsonHttpResult<ErrorBody>>
		MapErrorUpdate(Failure failure) => failure.Code switch
		{
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			"Guild.RoleNotFound" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			"Guild.NotAMember" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.MissingPermission" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.RoleHierarchyBlocked" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.CannotGrantPermissionsYouLack" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			_ => TypedResults.BadRequest(new ErrorBody(failure.Message)),
		};

	private static Results<NoContent, BadRequest<ErrorBody>, NotFound<ErrorBody>, JsonHttpResult<ErrorBody>>
		MapErrorDelete(Failure failure) => failure.Code switch
		{
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			"Guild.RoleNotFound" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			"Guild.NotAMember" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.MissingPermission" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.RoleHierarchyBlocked" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.CannotDeleteDefaultRole" => TypedResults.BadRequest(new ErrorBody(failure.Message)),
			_ => TypedResults.BadRequest(new ErrorBody(failure.Message)),
		};

	// ---- request shapes ----

	private sealed record CreateRoleRequest(
		string? Name,
		string? Color,
		long? Permissions,
		bool? IsHoisted,
		bool? IsMentionable);

	private sealed record UpdateRoleRequest(
		string? Name,
		string? Color,
		long? Permissions,
		int? Position,
		bool? IsHoisted,
		bool? IsMentionable);
}
