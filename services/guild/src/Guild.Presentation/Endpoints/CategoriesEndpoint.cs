using Carter;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Categories.Common;
using Guild.Application.Features.Categories.CreateCategory;
using Guild.Application.Features.Categories.DeleteCategory;
using Guild.Application.Features.Categories.UpdateCategory;
using Guild.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Guild.Presentation.Endpoints;

public sealed class CategoriesEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup("/guilds/{id:long}/categories");
		group.MapPost("/", CreateAsync);
		group.MapPatch("/{categoryId:long}", UpdateAsync);
		group.MapDelete("/{categoryId:long}", DeleteAsync);
	}

	private static async Task<Results<
		Created<CategoryResponse>,
		BadRequest<ErrorBody>,
		NotFound<ErrorBody>,
		JsonHttpResult<ErrorBody>>>
	CreateAsync(
		long id,
		CreateCategoryRequest request,
		ICommandHandler<CreateCategoryCommand, Result<CategoryResponse>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new CreateCategoryCommand(id, request.Name, request.Position),
			cancellationToken);

		return result.Succeeded
			? TypedResults.Created($"/v1/guilds/{id}/categories/{result.Value.Id}", result.Value)
			: MapErrorCreate(result.Error);
	}

	private static async Task<Results<
		Ok<CategoryResponse>,
		BadRequest<ErrorBody>,
		NotFound<ErrorBody>,
		JsonHttpResult<ErrorBody>>>
	UpdateAsync(
		long id,
		long categoryId,
		UpdateCategoryRequest request,
		ICommandHandler<UpdateCategoryCommand, Result<CategoryResponse>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new UpdateCategoryCommand(id, categoryId, request.Name, request.Position),
			cancellationToken);

		return result.Succeeded ? TypedResults.Ok(result.Value) : MapErrorUpdate(result.Error);
	}

	private static async Task<Results<
		NoContent,
		NotFound<ErrorBody>,
		JsonHttpResult<ErrorBody>>>
	DeleteAsync(
		long id,
		long categoryId,
		ICommandHandler<DeleteCategoryCommand, Result> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new DeleteCategoryCommand(id, categoryId),
			cancellationToken);

		return result.Succeeded ? TypedResults.NoContent() : MapErrorDelete(result.Error);
	}

	// ---- error mapping ----

	private static Results<Created<CategoryResponse>, BadRequest<ErrorBody>, NotFound<ErrorBody>, JsonHttpResult<ErrorBody>>
		MapErrorCreate(Failure failure) => failure.Code switch
		{
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			"Guild.NotAMember" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.MissingPermission" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			_ => TypedResults.BadRequest(new ErrorBody(failure.Message)),
		};

	private static Results<Ok<CategoryResponse>, BadRequest<ErrorBody>, NotFound<ErrorBody>, JsonHttpResult<ErrorBody>>
		MapErrorUpdate(Failure failure) => failure.Code switch
		{
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			"Guild.CategoryNotFound" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			"Guild.NotAMember" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.MissingPermission" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			_ => TypedResults.BadRequest(new ErrorBody(failure.Message)),
		};

	private static Results<NoContent, NotFound<ErrorBody>, JsonHttpResult<ErrorBody>>
		MapErrorDelete(Failure failure) => failure.Code switch
		{
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			"Guild.CategoryNotFound" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			"Guild.NotAMember" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			_ => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
		};

	// ---- request shapes (snake_case via ConfigureHttpJsonOptions) ----

	private sealed record CreateCategoryRequest(string? Name, int? Position);
	private sealed record UpdateCategoryRequest(string? Name, int? Position);
}
