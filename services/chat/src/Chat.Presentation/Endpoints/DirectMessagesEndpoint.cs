using Carter;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.DirectMessages.Common;
using Chat.Application.Features.DirectMessages.SendMessage;
using Chat.Domain.Results;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Chat.Presentation.Endpoints;

public sealed class DirectMessagesEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/channels/{channelId:long}/messages").WithTags("Direct Messages");
		group.MapPost("/", SendAsync);
	}

	private static async Task<Results<
		Created<DirectMessageResponse>,
		BadRequest<ErrorBody>,
		JsonHttpResult<ErrorBody>,
		NotFound<ErrorBody>>>
	SendAsync(
		long userId,
		SendDirectMessageRequest request,
		ICommandHandler<SendDirectMessageCommand, Result<DirectMessageResponse>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new SendDirectMessageCommand(
				RecipientId: userId,
				Content: request.Content,
				ReplyToId: request.ReplyToId,
				Nonce: request.Nonce),
			cancellationToken);

		if (result.Succeeded)
		{
			return TypedResults.Created(
				$"/dms/{userId}/messages/{result.Value.Id}",
				result.Value);
		}

		return MapError(result.Error);
	}

	private static Results<
		Created<DirectMessageResponse>,
		BadRequest<ErrorBody>,
		JsonHttpResult<ErrorBody>,
		NotFound<ErrorBody>>
	MapError(Failure failure)
	{
		var body = new ErrorBody(failure.Message);

		return failure.Code switch
		{
			"DirectMessage.ConversationNotFound" => TypedResults.NotFound(body),

			"DirectMessage.NotAFriend" => TypedResults.Json(
				body,
				statusCode: StatusCodes.Status403Forbidden),

			"DirectMessage.ContentRequired" => TypedResults.BadRequest(body),
			"DirectMessage.ContentTooLong" => TypedResults.BadRequest(body),
			"DirectMessage.InvalidId" => TypedResults.BadRequest(body),
			"DirectMessage.InvalidConversationId" => TypedResults.BadRequest(body),
			"DirectMessage.InvalidSenderId" => TypedResults.BadRequest(body),
			"DirectMessage.InvalidRecipientId" => TypedResults.BadRequest(body),
			"DirectMessage.CannotMessageSelf" => TypedResults.BadRequest(body),
			"DirectMessage.NonceTooLong" => TypedResults.BadRequest(body),

			_ => TypedResults.BadRequest(body)
		};
	}

	private sealed record SendDirectMessageRequest(
		string? Content,
		long? ReplyToId,
		string? Nonce);

	private sealed record ErrorBody(string Error);
}
