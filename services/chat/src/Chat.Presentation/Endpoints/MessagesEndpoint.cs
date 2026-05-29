using Carter;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.Messages.Common;
using Chat.Application.Features.Messages.SendMessage;
using Chat.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Chat.Presentation.Endpoints;

public sealed class MessagesEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup("/channels/{channelId:long}/messages");
		group.MapPost("/", SendAsync);
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
			new SendMessageCommand(channelId, request.Content, request.ReplyToId, request.Nonce),
			cancellationToken);

		return result.Succeeded
			? TypedResults.Created($"/channels/{channelId}/messages/{result.Value.Id}", result.Value)
			: MapError(result.Error);
	}

	private static Results<Created<MessageResponse>, BadRequest<ErrorBody>, JsonHttpResult<ErrorBody>, NotFound<ErrorBody>>
		MapError(Failure failure) => failure.Code switch
		{
			"Message.ChannelNotFound" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			"Message.NotAMember" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			"Message.MissingSendPermission" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			_ => TypedResults.BadRequest(new ErrorBody(failure.Message)),
		};

	private sealed record SendMessageRequest(
		string? Content,
		long? ReplyToId,
		string? Nonce);
}
