using Carter;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Invites.Common;
using Guild.Application.Features.Invites.CreateInvite;
using Guild.Application.Features.Invites.RevokeInvite;
using Guild.Application.Features.Invites.ListInvites;
using Guild.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Guild.Presentation.Endpoints;

public sealed class GuildInvitesEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup("/guilds/{id:long}/invites");
		group.MapPost("/", CreateAsync).ProducesGuildErrors();
		group.MapGet("/", ListAsync).ProducesGuildErrors();
		// DELETE here soft-revokes the invite (sets revoked), it does not hard-delete
		// the row; hard deletion of expired/revoked invites is the cleanup service's job
		group.MapDelete("/{code}", RevokeAsync).ProducesGuildErrors();
	}

	private static async Task<Results<Created<InviteDto>, JsonHttpResult<ErrorBody>>>
	CreateAsync(
		long id,
		CreateInviteRequest request,
		ICommandHandler<CreateInviteCommand, Result<InviteDto>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new CreateInviteCommand(id, request.MaxUses, request.ExpiresInHours),
			cancellationToken);

		if (result.Succeeded)
			return TypedResults.Created($"/v1/invites/{result.Value.Code}", result.Value);

		return EndpointResults.Problem(result.Error);
	}

	private static async Task<Results<Ok<IReadOnlyList<InviteDto>>, JsonHttpResult<ErrorBody>>>
	ListAsync(
		long id,
		IQueryHandler<ListInvitesQuery, Result<IReadOnlyList<InviteDto>>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(new ListInvitesQuery(id), cancellationToken);

		if (result.Succeeded)
			return TypedResults.Ok(result.Value);

		return EndpointResults.Problem(result.Error);
	}

	private static async Task<Results<NoContent, JsonHttpResult<ErrorBody>>>
	RevokeAsync(
		long id,
		string code,
		ICommandHandler<RevokeInviteCommand, Result> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(new RevokeInviteCommand(id, code), cancellationToken);

		if (result.Succeeded)
			return TypedResults.NoContent();

		return EndpointResults.Problem(result.Error);
	}

	private sealed record CreateInviteRequest(int? MaxUses, int? ExpiresInHours);
}
