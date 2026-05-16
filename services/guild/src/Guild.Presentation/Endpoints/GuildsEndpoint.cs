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

	private static async Task<Results<
		Created<GuildDto>,
		BadRequest<ErrorBody>>>
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
			: TypedResults.BadRequest(new ErrorBody(result.Error.Message));
	}

	private static async Task<Results<
		Ok<GuildDetailsDto>,
		NotFound<ErrorBody>,
		JsonHttpResult<ErrorBody>>>
	GetAsync(
		long id,
		IQueryHandler<GetGuildQuery, Result<GuildDetailsDto>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(new GetGuildQuery(id), cancellationToken);
		return result.Succeeded ? TypedResults.Ok(result.Value) : MapErrorGet(result.Error);
	}

	private static async Task<Results<
		Ok<GuildDto>,
		BadRequest<ErrorBody>,
		NotFound<ErrorBody>,
		JsonHttpResult<ErrorBody>>>
	UpdateAsync(
		long id,
		UpdateGuildRequest request,
		ICommandHandler<UpdateGuildCommand, Result<GuildDto>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new UpdateGuildCommand(id, request.Name, request.Description, request.IconUrl, request.BannerUrl),
			cancellationToken);

		return result.Succeeded ? TypedResults.Ok(result.Value) : MapErrorUpdate(result.Error);
	}

	private static async Task<Results<
		NoContent,
		NotFound<ErrorBody>,
		JsonHttpResult<ErrorBody>>>
	DeleteAsync(
		long id,
		ICommandHandler<DeleteGuildCommand, Result> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(new DeleteGuildCommand(id), cancellationToken);
		return result.Succeeded ? TypedResults.NoContent() : MapErrorDelete(result.Error);
	}

	private static async Task<Results<
		Ok<GuildDto>,
		BadRequest<ErrorBody>,
		NotFound<ErrorBody>,
		JsonHttpResult<ErrorBody>>>
	TransferOwnershipAsync(
		long id,
		TransferOwnershipRequest request,
		ICommandHandler<TransferOwnershipCommand, Result<GuildDto>> handler,
		CancellationToken cancellationToken)
	{
		if (!long.TryParse(request.NewOwnerId, out var newOwnerId))
			return TypedResults.BadRequest(new ErrorBody("new_owner_id must be a snowflake string."));

		var result = await handler.HandleAsync(
			new TransferOwnershipCommand(id, newOwnerId),
			cancellationToken);

		return result.Succeeded ? TypedResults.Ok(result.Value) : MapErrorTransfer(result.Error);
	}

	// ---- error mapping ----
	//
	// each endpoint has its own discriminated union return type, so we need
	// separate mapper methods so the compiler can pick the correct implicit
	// conversion to each variant. the mapping logic itself is shared

	private static Results<Ok<GuildDetailsDto>, NotFound<ErrorBody>, JsonHttpResult<ErrorBody>>
		MapErrorGet(Failure failure) => failure.Code switch
		{
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			"Guild.NotAMember" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			_ => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
		};

	private static Results<Ok<GuildDto>, BadRequest<ErrorBody>, NotFound<ErrorBody>, JsonHttpResult<ErrorBody>>
		MapErrorUpdate(Failure failure) => failure.Code switch
		{
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			"Guild.TargetNotAMember" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			"Guild.NotAMember" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.NotTheOwner" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.MissingPermission" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			_ => TypedResults.BadRequest(new ErrorBody(failure.Message)),
		};

	private static Results<NoContent, NotFound<ErrorBody>, JsonHttpResult<ErrorBody>>
		MapErrorDelete(Failure failure) => failure.Code switch
		{
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			"Guild.NotTheOwner" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			_ => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
		};

	private static Results<Ok<GuildDto>, BadRequest<ErrorBody>, NotFound<ErrorBody>, JsonHttpResult<ErrorBody>>
		MapErrorTransfer(Failure failure) => failure.Code switch
		{
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			"Guild.TargetNotAMember" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			"Guild.NotTheOwner" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			_ => TypedResults.BadRequest(new ErrorBody(failure.Message)),
		};

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
