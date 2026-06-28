using Carter;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Channels.Common;
using Guild.Application.Features.Channels.CreateChannel;
using Guild.Application.Features.Channels.DeleteChannel;
using Guild.Application.Features.Channels.ListChannels;
using Guild.Application.Features.Channels.UpdateChannel;
using Guild.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Guild.Presentation.Endpoints;

public sealed class ChannelsEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup("/guilds/{id:long}/channels");
		group.MapGet("/", ListAsync);
		group.MapPost("/", CreateAsync);
		group.MapPatch("/{channelId:long}", UpdateAsync);
		group.MapDelete("/{channelId:long}", DeleteAsync);
	}

	private static async Task<Results<Ok<IReadOnlyList<ChannelResponse>>, JsonHttpResult<ErrorBody>>>
	ListAsync(
		long id,
		IQueryHandler<ListChannelsQuery, Result<ChannelListResponse>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(new ListChannelsQuery(id), cancellationToken);
		return result.Succeeded
			? TypedResults.Ok(result.Value.Items)
			: EndpointResults.Problem(result.Error);
	}

	private static async Task<Results<Created<ChannelResponse>, JsonHttpResult<ErrorBody>>>
	CreateAsync(
		long id,
		CreateChannelRequest request,
		ICommandHandler<CreateChannelCommand, Result<ChannelResponse>> handler,
		CancellationToken cancellationToken)
	{
		long? categoryId = null;
		if (request.CategoryId is { Length: > 0 } raw)
		{
			if (!long.TryParse(raw, out var parsed))
				return TypedResults.Json(new ErrorBody("category_id must be a snowflake string."), statusCode: StatusCodes.Status400BadRequest);
			categoryId = parsed;
		}

		var result = await handler.HandleAsync(
			new CreateChannelCommand(
				GuildId: id,
				Name: request.Name,
				Type: request.Type,
				CategoryId: categoryId,
				Topic: request.Topic,
				Position: request.Position,
				IsNsfw: request.IsNsfw,
				SlowmodeSeconds: request.SlowmodeSeconds),
			cancellationToken);

		return result.Succeeded
			? TypedResults.Created($"/v1/guilds/{id}/channels/{result.Value.Id}", result.Value)
			: EndpointResults.Problem(result.Error);
	}

	private static async Task<Results<Ok<ChannelResponse>, JsonHttpResult<ErrorBody>>>
	UpdateAsync(
		long id,
		long channelId,
		UpdateChannelRequest request,
		ICommandHandler<UpdateChannelCommand, Result<ChannelResponse>> handler,
		CancellationToken cancellationToken)
	{
		// PATCH semantics: absent field == "no change". since System.Text.Json
		// cannot distinguish "absent" from "explicit null" for reference types,
		// callers signal "clear topic" by sending an empty string and "remove
		// from category" by sending "" for category_id
		var topicProvided = request.Topic is not null;
		var categoryProvided = request.CategoryId is not null;

		long? categoryId = null;
		if (categoryProvided && request.CategoryId is { Length: > 0 } raw)
		{
			if (!long.TryParse(raw, out var parsed))
				return TypedResults.Json(new ErrorBody("category_id must be a snowflake string or empty to clear."), statusCode: StatusCodes.Status400BadRequest);
			categoryId = parsed;
		}

		var result = await handler.HandleAsync(
			new UpdateChannelCommand(
				GuildId: id,
				ChannelId: channelId,
				Name: request.Name,
				Topic: request.Topic,
				Position: request.Position,
				CategoryId: categoryId,
				CategoryIdProvided: categoryProvided,
				TopicProvided: topicProvided),
			cancellationToken);

		return result.Succeeded ? TypedResults.Ok(result.Value) : EndpointResults.Problem(result.Error);
	}

	private static async Task<Results<NoContent, JsonHttpResult<ErrorBody>>>
	DeleteAsync(
		long id,
		long channelId,
		ICommandHandler<DeleteChannelCommand, Result> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new DeleteChannelCommand(id, channelId),
			cancellationToken);

		return result.Succeeded ? TypedResults.NoContent() : EndpointResults.Problem(result.Error);
	}

	// ---- error mapping ----





	// ---- request shapes (snake_case via ConfigureHttpJsonOptions) ----

	private sealed record CreateChannelRequest(
		string? Name,
		string? Type,
		string? CategoryId,
		string? Topic,
		int? Position,
		bool? IsNsfw,
		int? SlowmodeSeconds);

	private sealed record UpdateChannelRequest(
		string? Name,
		string? Topic,
		int? Position,
		string? CategoryId);
}
