using Carter;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Bans.Common;
using Guild.Application.Features.Bans.ListBans;
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
}
