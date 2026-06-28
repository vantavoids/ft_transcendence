using Carter;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Roles.Common;
using Guild.Application.Features.Roles.CreateRole;
using Guild.Application.Features.Roles.DeleteRole;
using Guild.Application.Features.Roles.ListRoles;
using Guild.Application.Features.Roles.ReorderRoles;
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
		group.MapGet("/", ListAsync).ProducesGuildErrors();
		group.MapPost("/", CreateAsync).ProducesGuildErrors();
		group.MapPatch("/", ReorderAsync).ProducesGuildErrors();
		group.MapPatch("/{roleId:long}", UpdateAsync).ProducesGuildErrors();
		group.MapDelete("/{roleId:long}", DeleteAsync).ProducesGuildErrors();
	}

	private static async Task<Results<Ok<IReadOnlyList<RoleResponse>>, JsonHttpResult<ErrorBody>>>
	ListAsync(
		long id,
		IQueryHandler<ListRolesQuery, Result<RoleListResponse>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(new ListRolesQuery(id), cancellationToken);
		return result.Succeeded
			? TypedResults.Ok(result.Value.Items)
			: EndpointResults.Problem(result.Error);
	}

	private static async Task<Results<Created<RoleResponse>, JsonHttpResult<ErrorBody>>>
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
			: EndpointResults.Problem(result.Error);
	}

	private static async Task<Results<Ok<RoleResponse>, JsonHttpResult<ErrorBody>>>
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
				IsHoisted: request.IsHoisted,
				IsMentionable: request.IsMentionable),
			cancellationToken);

		return result.Succeeded
			? TypedResults.Ok(result.Value)
			: EndpointResults.Problem(result.Error);
	}

	private static async Task<Results<Ok<IReadOnlyList<RoleResponse>>, JsonHttpResult<ErrorBody>>>
	ReorderAsync(
		long id,
		IReadOnlyList<ReorderRoleEntry>? request,
		ICommandHandler<ReorderRolesCommand, Result<RoleListResponse>> handler,
		CancellationToken cancellationToken)
	{
		if (request is null)
			return TypedResults.Json(new ErrorBody("Request body must be a JSON array of { id, position }."), statusCode: StatusCodes.Status400BadRequest);

		var moves = new List<RolePositionEntry>(request.Count);
		foreach (var entry in request)
		{
			if (!long.TryParse(entry.Id, out var roleId))
				return TypedResults.Json(new ErrorBody("Each role id must be a numeric snowflake string."), statusCode: StatusCodes.Status400BadRequest);
			moves.Add(new RolePositionEntry(roleId, entry.Position));
		}

		var result = await handler.HandleAsync(new ReorderRolesCommand(id, moves), cancellationToken);
		return result.Succeeded
			? TypedResults.Ok(result.Value.Items)
			: EndpointResults.Problem(result.Error);
	}

	private static async Task<Results<NoContent, JsonHttpResult<ErrorBody>>>
	DeleteAsync(
		long id,
		long roleId,
		ICommandHandler<DeleteRoleCommand, Result> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(new DeleteRoleCommand(id, roleId), cancellationToken);
		return result.Succeeded
			? TypedResults.NoContent()
			: EndpointResults.Problem(result.Error);
	}

	// ---- error mapping ----






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
		bool? IsHoisted,
		bool? IsMentionable);

	// snowflake id arrives as a quoted string (JS precision); position is the
	// requested 1..N slot. parsed/validated into RolePositionEntry by ReorderAsync
	private sealed record ReorderRoleEntry(string Id, int Position);
}
