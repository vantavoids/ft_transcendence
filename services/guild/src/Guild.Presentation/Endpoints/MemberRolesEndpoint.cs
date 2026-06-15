using Carter;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Membership.AssignRole;
using Guild.Application.Features.Membership.UnassignRole;
using Guild.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Guild.Presentation.Endpoints;

public sealed class MemberRolesEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup("/guilds/{id:long}/members/{userId:long}/roles");
		group.MapPut("/{roleId:long}", AssignAsync);
		group.MapDelete("/{roleId:long}", UnassignAsync);
	}

	private static async Task<Results<
		NoContent,
		BadRequest<ErrorBody>,
		NotFound<ErrorBody>,
		JsonHttpResult<ErrorBody>>>
	AssignAsync(
		long id,
		long userId,
		long roleId,
		ICommandHandler<AssignRoleCommand, Result> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new AssignRoleCommand(id, userId, roleId), cancellationToken);

		if (result.Succeeded)
			return TypedResults.NoContent();

		return result.Error.Code switch
		{
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(result.Error.Message)),
			"Guild.RoleNotFound" => TypedResults.NotFound(new ErrorBody(result.Error.Message)),
			"Guild.TargetNotAMember" => TypedResults.NotFound(new ErrorBody(result.Error.Message)),
			"Guild.NotAMember" => TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.MissingPermission" => TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.RoleHierarchyBlocked" => TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.CannotGrantPermissionsYouLack" => TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.CannotAssignDefaultRole" => TypedResults.BadRequest(new ErrorBody(result.Error.Message)),
			_ => TypedResults.BadRequest(new ErrorBody(result.Error.Message)),
		};
	}

	private static async Task<Results<
		NoContent,
		BadRequest<ErrorBody>,
		NotFound<ErrorBody>,
		JsonHttpResult<ErrorBody>>>
	UnassignAsync(
		long id,
		long userId,
		long roleId,
		ICommandHandler<UnassignRoleCommand, Result> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new UnassignRoleCommand(id, userId, roleId), cancellationToken);

		if (result.Succeeded)
			return TypedResults.NoContent();

		return result.Error.Code switch
		{
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(result.Error.Message)),
			"Guild.RoleNotFound" => TypedResults.NotFound(new ErrorBody(result.Error.Message)),
			"Guild.TargetNotAMember" => TypedResults.NotFound(new ErrorBody(result.Error.Message)),
			"Guild.RoleAssignmentNotFound" => TypedResults.NotFound(new ErrorBody(result.Error.Message)),
			"Guild.NotAMember" => TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.MissingPermission" => TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.RoleHierarchyBlocked" => TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.CannotUnassignDefaultRole" => TypedResults.BadRequest(new ErrorBody(result.Error.Message)),
			_ => TypedResults.BadRequest(new ErrorBody(result.Error.Message)),
		};
	}
}
