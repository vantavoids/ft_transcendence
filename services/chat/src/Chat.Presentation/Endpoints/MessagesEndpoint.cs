using Carter;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.Messages.Common;
using Chat.Application.Features.Messages.DeleteMessage;
using Chat.Application.Features.Messages.EditMessage;
using Chat.Application.Features.Messages.GetChannelMessages;
using Chat.Application.Features.Messages.SendMessage;
using Chat.Domain.Results;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Chat.Presentation.Endpoints;

public sealed class MessagesEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder endpoints)
	{
		var channelGroup = endpoints.MapGroup("/channels/{channelId:long}/messages");
		channelGroup.MapGet("/", GetChannelHistoryAsync);
		channelGroup.MapPost("/", SendAsync);

		var messageGroup = endpoints.MapGroup("/messages");
		messageGroup.MapPatch("/{messageId:long}", EditAsync);
		messageGroup.MapDelete("/{messageId:long}", DeleteAsync);
	}

	private static async Task<Results<
		Ok<IReadOnlyList<MessageResponse>>,
		JsonHttpResult<ErrorBody>,
		NotFound<ErrorBody>>>
	GetChannelHistoryAsync(
		long channelId,
		DateTimeOffset? before_time,
		int? limit,
		IQueryHandler<GetChannelMessagesQuery, Result<IReadOnlyList<MessageResponse>>> handler,
		CancellationToken cancellationToken)
	{
		var clampedLimit = Math.Clamp(limit ?? 50, 1, 100);
		var result = await handler.HandleAsync(
			new GetChannelMessagesQuery(channelId, before_time, clampedLimit),
			cancellationToken);

		return result.Succeeded
			? TypedResults.Ok(result.Value)
			: MapGetChannelHistoryError(result.Error);
	}

	private static async Task<Results<
		Created<MessageResponse>,
		BadRequest<ErrorBody>,
		JsonHttpResult<ErrorBody>,
		NotFound<ErrorBody>>>
	SendAsync(
		long channelId,
		SendMessageRequest request,
		ICommandHandler<SendMessageCommand, Result<MessageResponse>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new SendMessageCommand(
				channelId,
				request.Content,
				request.ReplyToId,
				request.AttachmentIds ?? [],
				request.Nonce),
			cancellationToken);

		return result.Succeeded
			? TypedResults.Created($"/channels/{channelId}/messages/{result.Value.Id}", result.Value)
			: MapSendError(result.Error);
	}

	private static async Task<Results<
		Ok<EditMessageResponse>,
		BadRequest<ErrorBody>,
		JsonHttpResult<ErrorBody>,
		NotFound<ErrorBody>>>
	EditAsync(
		long messageId,
		EditMessageRequest request,
		ICommandHandler<EditMessageCommand, Result<EditMessageResponse>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new EditMessageCommand(messageId, request.Content),
			cancellationToken);

		return result.Succeeded
			? TypedResults.Ok(result.Value)
			: MapEditError(result.Error);
	}

	private static async Task<Results<
		NoContent,
		JsonHttpResult<ErrorBody>,
		NotFound<ErrorBody>>>
	DeleteAsync(
		long messageId,
		ICommandHandler<DeleteMessageCommand, Result> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new DeleteMessageCommand(messageId),
			cancellationToken);

		return result.Succeeded
			? TypedResults.NoContent()
			: MapDeleteError(result.Error);
	}

	private static Results<Created<MessageResponse>, BadRequest<ErrorBody>, JsonHttpResult<ErrorBody>, NotFound<ErrorBody>>
		MapSendError(Failure failure) => failure.Code switch
		{
			"Message.ChannelNotFound" or
			"Attachment.InvalidReference" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			"Message.NotAMember" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			"Message.MissingSendPermission" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			_ => TypedResults.BadRequest(new ErrorBody(failure.Message)),
		};

	private static Results<Ok<EditMessageResponse>, BadRequest<ErrorBody>, JsonHttpResult<ErrorBody>, NotFound<ErrorBody>>
		MapEditError(Failure failure) => failure.Code switch
		{
			"Message.NotFound" or "Message.AlreadyDeleted" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			"Message.NotAuthor" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			_ => TypedResults.BadRequest(new ErrorBody(failure.Message)),
		};

	private static Results<Ok<IReadOnlyList<MessageResponse>>, JsonHttpResult<ErrorBody>, NotFound<ErrorBody>>
		MapGetChannelHistoryError(Failure failure) => failure.Code switch
		{
			"Message.ChannelNotFound" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			_ => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
		};

	private static Results<NoContent, JsonHttpResult<ErrorBody>, NotFound<ErrorBody>>
		MapDeleteError(Failure failure) => failure.Code switch
		{
			"Message.NotFound" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			"Message.MissingManagePermission" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			_ => TypedResults.NotFound(new ErrorBody(failure.Message)),
		};

	private sealed record SendMessageRequest(
		string? Content,
		long? ReplyToId,
		IReadOnlyList<long>? AttachmentIds,
		string? Nonce);
	private sealed record EditMessageRequest(string? Content);
}
