using Carter;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Channels.Permissions.DeleteOverwrite;
using Guild.Application.Features.Channels.Permissions.GetOverwrites;
using Guild.Application.Features.Channels.Permissions.PutOverwrite;
using Guild.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Guild.Presentation.Endpoints;

public sealed class ChannelPermissionsEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup("/channels/{channelId:long}/permissions");
		group.MapGet("/", ListAsync);
		group.MapPut("/{targetId:long}", PutAsync);
		group.MapDelete("/{targetId:long}", DeleteAsync);
	}

	private static async Task<Results<Ok<IReadOnlyList<OverwriteItem>>, JsonHttpResult<ErrorBody>>>
	ListAsync(
		long channelId,
		IQueryHandler<GetOverwritesQuery, Result<OverwritesResponse>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(new GetOverwritesQuery(channelId), cancellationToken);
		return result.Succeeded
			? TypedResults.Ok(result.Value.Items)
			: EndpointResults.Problem(result.Error);
	}

	private static async Task<Results<NoContent, JsonHttpResult<ErrorBody>>>
	PutAsync(
		long channelId,
		long targetId,
		PutOverwriteRequest request,
		ICommandHandler<PutOverwriteCommand, Result> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new PutOverwriteCommand(
				ChannelId: channelId,
				TargetId: targetId,
				TargetType: request.TargetType,
				Allow: request.Allow ?? 0L,
				Deny: request.Deny ?? 0L),
			cancellationToken);

		return result.Succeeded
			? TypedResults.NoContent()
			: EndpointResults.Problem(result.Error);
	}

	private static async Task<Results<NoContent, JsonHttpResult<ErrorBody>>>
	DeleteAsync(
		long channelId,
		long targetId,
		ICommandHandler<DeleteOverwriteCommand, Result> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new DeleteOverwriteCommand(channelId, targetId),
			cancellationToken);

		return result.Succeeded ? TypedResults.NoContent() : EndpointResults.Problem(result.Error);
	}

	// ---- error mapping ----




	// ---- request shape ----

	private sealed record PutOverwriteRequest(
		string? TargetType,
		long? Allow,
		long? Deny);
}
