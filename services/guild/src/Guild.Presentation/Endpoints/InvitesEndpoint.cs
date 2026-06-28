using Carter;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Guilds.Common;
using Guild.Application.Features.Invites.Common;
using Guild.Application.Features.Invites.GetInvitePreview;
using Guild.Application.Features.Membership.JoinByInviteCode;
using Guild.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Guild.Presentation.Endpoints;

/// <summary>
/// preview + accept flows for invite-link landings. these live outside
/// <c>/guilds/{id}/...</c> because the client only knows the code, not the
/// guild id, until preview returns it
/// </summary>
public sealed class InvitesEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup("/invites/{code}");
		group.MapGet("/", PreviewAsync).ProducesGuildErrors();
		group.MapPost("/join", JoinAsync).ProducesGuildErrors().ProducesConflict();
	}

	private static async Task<Results<Ok<InvitePreviewDto>, JsonHttpResult<ErrorBody>>>
	PreviewAsync(
		string code,
		IQueryHandler<GetInvitePreviewQuery, Result<InvitePreviewDto>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(new GetInvitePreviewQuery(code), cancellationToken);

		if (result.Succeeded)
			return TypedResults.Ok(result.Value);

		return TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status404NotFound);
	}

	private static async Task<Results<Ok<GuildDto>, JsonHttpResult<ErrorBody>>>
	JoinAsync(
		string code,
		ICommandHandler<JoinByInviteCodeCommand, Result<GuildDto>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new JoinByInviteCodeCommand(code, ExpectedGuildId: null),
			cancellationToken);

		if (result.Succeeded)
			return TypedResults.Ok(result.Value);

		return EndpointResults.Problem(result.Error);
	}
}
