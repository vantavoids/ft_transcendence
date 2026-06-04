using Carter;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Guilds.Common;
using Guild.Application.Features.Membership.JoinByInviteCode;
using Guild.Application.Features.Membership.LeaveGuild;
using Guild.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Guild.Presentation.Endpoints;

public sealed class MembershipEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup("/guilds/{id:long}");
		group.MapPost("/join", JoinAsync);
		group.MapPost("/leave", LeaveAsync);
	}

	private static async Task<Results<
		Ok<GuildDto>,
		BadRequest<ErrorBody>,
		NotFound<ErrorBody>,
		Conflict<ErrorBody>,
		JsonHttpResult<ErrorBody>>>
	JoinAsync(
		long id,
		JoinByPathRequest request,
		ICommandHandler<JoinByInviteCodeCommand, Result<GuildDto>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new JoinByInviteCodeCommand(request.InviteCode, ExpectedGuildId: id),
			cancellationToken);

		if (result.Succeeded)
			return TypedResults.Ok(result.Value);

		return result.Error.Code switch
		{
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(result.Error.Message)),
			"Guild.AlreadyMember" => TypedResults.Conflict(new ErrorBody(result.Error.Message)),
			_ => TypedResults.BadRequest(new ErrorBody(result.Error.Message)),
		};
	}

	private static async Task<Results<
		NoContent,
		BadRequest<ErrorBody>,
		NotFound<ErrorBody>,
		JsonHttpResult<ErrorBody>>>
	LeaveAsync(
		long id,
		ICommandHandler<LeaveGuildCommand, Result> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(new LeaveGuildCommand(id), cancellationToken);

		if (result.Succeeded)
			return TypedResults.NoContent();

		return result.Error.Code switch
		{
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(result.Error.Message)),
			"Guild.OwnerCannotLeave" => TypedResults.BadRequest(new ErrorBody(result.Error.Message)),
			"Guild.NotAMember" => TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status403Forbidden),
			_ => TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status403Forbidden),
		};
	}

	private sealed record JoinByPathRequest(string? InviteCode);
}
