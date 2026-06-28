using Carter;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Guilds.Common;
using Guild.Application.Features.Guilds.CreateGuild;
using Guild.Application.Features.Guilds.DeleteGuild;
using Guild.Application.Features.Guilds.GetGuild;
using Guild.Application.Features.Guilds.TransferOwnership;
using Guild.Application.Features.Guilds.UpdateGuild;
using Guild.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Guild.Presentation.Endpoints;

public sealed class GuildsEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup("/guilds");
		group.MapPost("/", CreateAsync);
		group.MapGet("/{id:long}", GetAsync);
		group.MapPatch("/{id:long}", UpdateAsync);
		group.MapDelete("/{id:long}", DeleteAsync);
		group.MapPatch("/{id:long}/owner", TransferOwnershipAsync);
	}

	private static async Task<Results<Created<GuildDto>, JsonHttpResult<ErrorBody>>>
	CreateAsync(
		CreateGuildRequest request,
		ICommandHandler<CreateGuildCommand, Result<GuildDto>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new CreateGuildCommand(request.Name, request.Description, request.IconUrl, request.BannerUrl),
			cancellationToken);

		return result.Succeeded
			? TypedResults.Created($"/v1/guilds/{result.Value.Id}", result.Value)
			: TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status400BadRequest);
	}

	private static async Task<Results<Ok<GuildDetailsDto>, JsonHttpResult<ErrorBody>>>
	GetAsync(
		long id,
		IQueryHandler<GetGuildQuery, Result<GuildDetailsDto>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(new GetGuildQuery(id), cancellationToken);
		return result.Succeeded ? TypedResults.Ok(result.Value) : EndpointResults.Problem(result.Error);
	}

	private static async Task<Results<Ok<GuildDto>, JsonHttpResult<ErrorBody>>>
	UpdateAsync(
		long id,
		UpdateGuildRequest request,
		ICommandHandler<UpdateGuildCommand, Result<GuildDto>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new UpdateGuildCommand(id, request.Name, request.Description, request.IconUrl, request.BannerUrl),
			cancellationToken);

		return result.Succeeded ? TypedResults.Ok(result.Value) : EndpointResults.Problem(result.Error);
	}

	private static async Task<Results<NoContent, JsonHttpResult<ErrorBody>>>
	DeleteAsync(
		long id,
		ICommandHandler<DeleteGuildCommand, Result> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(new DeleteGuildCommand(id), cancellationToken);
		return result.Succeeded ? TypedResults.NoContent() : EndpointResults.Problem(result.Error);
	}

	private static async Task<Results<Ok<GuildDto>, JsonHttpResult<ErrorBody>>>
	TransferOwnershipAsync(
		long id,
		TransferOwnershipRequest request,
		ICommandHandler<TransferOwnershipCommand, Result<GuildDto>> handler,
		CancellationToken cancellationToken)
	{
		if (!long.TryParse(request.NewOwnerId, out var newOwnerId))
			return TypedResults.Json(new ErrorBody("new_owner_id must be a snowflake string."), statusCode: StatusCodes.Status400BadRequest);

		var result = await handler.HandleAsync(
			new TransferOwnershipCommand(id, newOwnerId),
			cancellationToken);

		return result.Succeeded ? TypedResults.Ok(result.Value) : EndpointResults.Problem(result.Error);
	}

	// ---- error mapping ----
	//
	// each endpoint has its own discriminated union return type, so we need
	// separate mapper methods so the compiler can pick the correct implicit
	// conversion to each variant. the mapping logic itself is shared





	// ---- request shapes (snake_case via ConfigureHttpJsonOptions) ----

	private sealed record CreateGuildRequest(
		string? Name,
		string? Description,
		string? IconUrl,
		string? BannerUrl);

	private sealed record UpdateGuildRequest(
		string? Name,
		string? Description,
		string? IconUrl,
		string? BannerUrl);

	private sealed record TransferOwnershipRequest(string NewOwnerId);
}
