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
	public void AddRoutes(IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup("/guilds/{id:long}/bans");
		group.MapGet("/", ListAsync);
		group.MapPost("/{userId:long}", BanAsync);
		group.MapDelete("/{userId:long}", UnbanAsync);
	}

	private static async Task<Results<Ok<IReadOnlyList<BanResponse>>, JsonHttpResult<ErrorBody>>>
	ListAsync(
		long id,
		string? after,
		int? limit,
		IQueryHandler<ListBansQuery, Result<BanListResponse>> handler,
		CancellationToken cancellationToken)
	{
		var (afterCursor, effectiveLimit, error) = Pagination.Parse(after, limit);
		if (error is not null)
			return error;

		var result = await handler.HandleAsync(
			new ListBansQuery(id, afterCursor, effectiveLimit),
			cancellationToken);

		if (result.Succeeded)
			return TypedResults.Ok(result.Value.Items);

		return EndpointResults.Problem(result.Error);
	}

	private static async Task<Results<NoContent, JsonHttpResult<ErrorBody>>>
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

		return EndpointResults.Problem(result.Error);
	}

	private static async Task<Results<NoContent, JsonHttpResult<ErrorBody>>>
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

		return EndpointResults.Problem(result.Error);
	}

	private sealed record BanRequest(string? Reason);
}
