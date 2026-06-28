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
		group.MapPut("/{roleId:long}", AssignAsync).ProducesGuildErrors();
		group.MapDelete("/{roleId:long}", UnassignAsync).ProducesGuildErrors();
	}

	private static async Task<Results<NoContent, JsonHttpResult<ErrorBody>>>
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

		return EndpointResults.Problem(result.Error);
	}

	private static async Task<Results<NoContent, JsonHttpResult<ErrorBody>>>
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

		return EndpointResults.Problem(result.Error);
	}
}
