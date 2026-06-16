using Carter;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Bans.BanMember;
using Guild.Application.Features.Bans.Common;
using Guild.Application.Features.Bans.ListBans;
using Guild.Application.Features.Bans.UnbanMember;
using Guild.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Guild.Presentation.Endpoints;

public sealed class BansEndpoint : ICarterModule
{
	private const int DefaultListLimit = 50;
	private const int MaxListLimit = 100;

	public void AddRoutes(IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup("/guilds/{id:long}/bans");
		group.MapGet("/", ListAsync);
		group.MapPost("/{userId:long}", BanAsync);
		group.MapDelete("/{userId:long}", UnbanAsync);
	}

	private static async Task<Results<
		Ok<IReadOnlyList<BanResponse>>,
		BadRequest<ErrorBody>,
		NotFound<ErrorBody>,
		JsonHttpResult<ErrorBody>>>
	ListAsync(
		long id,
		string? after,
		int? limit,
		IQueryHandler<ListBansQuery, Result<BanListResponse>> handler,
		CancellationToken cancellationToken)
	{
		long? afterCursor = null;
		if (after is not null)
		{
			if (!long.TryParse(after, out var parsed) || parsed <= 0)
				return TypedResults.BadRequest(new ErrorBody("after must be a positive snowflake."));
			afterCursor = parsed;
		}

		var effectiveLimit = limit ?? DefaultListLimit;
		if (effectiveLimit <= 0 || effectiveLimit > MaxListLimit)
			return TypedResults.BadRequest(new ErrorBody($"limit must be between 1 and {MaxListLimit}."));

		var result = await handler.HandleAsync(
			new ListBansQuery(id, afterCursor, effectiveLimit),
			cancellationToken);

		if (result.Succeeded)
			return TypedResults.Ok(result.Value.Items);

		return result.Error.Code switch
		{
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(result.Error.Message)),
			"Guild.NotAMember" => TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.MissingPermission" => TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status403Forbidden),
			_ => TypedResults.BadRequest(new ErrorBody(result.Error.Message)),
		};
	}

	private static async Task<Results<
		NoContent,
		BadRequest<ErrorBody>,
		NotFound<ErrorBody>,
		Conflict<ErrorBody>,
		JsonHttpResult<ErrorBody>>>
	BanAsync(
		long id,
		long userId,
		BanRequest? request,
		ICommandHandler<BanMemberCommand, Result> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new BanMemberCommand(id, userId, request?.Reason),
			cancellationToken);

		if (result.Succeeded)
			return TypedResults.NoContent();

		return result.Error.Code switch
		{
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(result.Error.Message)),
			"Guild.NotAMember" => TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.MissingPermission" => TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.CannotBanOwner" => TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.RoleHierarchyBlocked" => TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.CannotBanSelf" => TypedResults.BadRequest(new ErrorBody(result.Error.Message)),
			"Guild.BanReasonTooLong" => TypedResults.BadRequest(new ErrorBody(result.Error.Message)),
			"Guild.AlreadyBanned" => TypedResults.Conflict(new ErrorBody(result.Error.Message)),
			_ => TypedResults.BadRequest(new ErrorBody(result.Error.Message)),
		};
	}

	private static async Task<Results<
		NoContent,
		BadRequest<ErrorBody>,
		NotFound<ErrorBody>,
		JsonHttpResult<ErrorBody>>>
	UnbanAsync(
		long id,
		long userId,
		ICommandHandler<UnbanMemberCommand, Result> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new UnbanMemberCommand(id, userId), cancellationToken);

		if (result.Succeeded)
			return TypedResults.NoContent();

		return result.Error.Code switch
		{
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(result.Error.Message)),
			"Guild.BanNotFound" => TypedResults.NotFound(new ErrorBody(result.Error.Message)),
			"Guild.NotAMember" => TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.MissingPermission" => TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status403Forbidden),
			_ => TypedResults.BadRequest(new ErrorBody(result.Error.Message)),
		};
	}

	private sealed record BanRequest(string? Reason);
}
