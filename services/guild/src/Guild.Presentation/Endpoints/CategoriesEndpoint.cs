using Carter;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Categories.Common;
using Guild.Application.Features.Categories.CreateCategory;
using Guild.Application.Features.Categories.DeleteCategory;
using Guild.Application.Features.Categories.ListCategories;
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
		group.MapGet("/", ListAsync).ProducesGuildErrors();
		group.MapPost("/", CreateAsync).ProducesGuildErrors();
		group.MapPatch("/{categoryId:long}", UpdateAsync).ProducesGuildErrors();
		group.MapDelete("/{categoryId:long}", DeleteAsync).ProducesGuildErrors();
	}

	private static async Task<Results<Ok<IReadOnlyList<CategoryResponse>>, JsonHttpResult<ErrorBody>>>
	ListAsync(
		long id,
		IQueryHandler<ListCategoriesQuery, Result<CategoryListResponse>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(new ListCategoriesQuery(id), cancellationToken);
		return result.Succeeded
			? TypedResults.Ok(result.Value.Items)
			: EndpointResults.Problem(result.Error);
	}

	private static async Task<Results<Created<CategoryResponse>, JsonHttpResult<ErrorBody>>>
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
			: EndpointResults.Problem(result.Error);
	}

	private static async Task<Results<Ok<CategoryResponse>, JsonHttpResult<ErrorBody>>>
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

		return result.Succeeded ? TypedResults.Ok(result.Value) : EndpointResults.Problem(result.Error);
	}

	private static async Task<Results<NoContent, JsonHttpResult<ErrorBody>>>
	DeleteAsync(
		long id,
		long categoryId,
		ICommandHandler<DeleteCategoryCommand, Result> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new DeleteCategoryCommand(id, categoryId),
			cancellationToken);

		return result.Succeeded ? TypedResults.NoContent() : EndpointResults.Problem(result.Error);
	}

	// ---- error mapping ----




	// ---- request shapes (snake_case via ConfigureHttpJsonOptions) ----

	private sealed record CreateCategoryRequest(string? Name, int? Position);
	private sealed record UpdateCategoryRequest(string? Name, int? Position);
}
