using Carter;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.DirectMessages.Common;
using Chat.Application.Features.DirectMessages.SendMessage;
using Chat.Application.Features.DirectMessages.ListMessages;
using Chat.Domain.Results;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Chat.Presentation.Endpoints;

public sealed class DirectMessagesEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/dms/{userId:long}/messages").WithTags("Direct Messages");
		group.MapPost("/", SendAsync);
		group.MapGet("/", ListAsync);
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

	private static async Task<Results<
		Ok<IReadOnlyList<DirectMessageResponse>>,
	BadRequest<ErrorBody>>>
		ListAsync(
				long userId,
				DateTimeOffset? before_time,
				int? limit,
				IQueryHandler<ListDirectMessagesQuery, Result<IReadOnlyList<DirectMessageResponse>>> handler,
				CancellationToken cancellationToken)
		{
			var result = await handler.HandleAsync(
					new ListDirectMessagesQuery(
						RecipientId: userId,
						Limit: limit ?? 50,
						BeforeTime: before_time),
					cancellationToken);

			if (result.Succeeded)
				return TypedResults.Ok(result.Value);

			return TypedResults.BadRequest(new ErrorBody(result.Error.Message));
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

			// "DirectMessage.ContentRequired"
			// "DirectMessage.ContentTooLong"
			// "DirectMessage.InvalidId"
			// "DirectMessage.InvalidConversationId"
			// "DirectMessage.InvalidSenderId"
			// "DirectMessage.InvalidRecipientId"
			// "DirectMessage.InvalidReplyTarget"
			// "DirectMessage.CannotMessageSelf"
			// "DirectMessage.NonceTooLong"
			_ => TypedResults.BadRequest(body)
		};
	}

	private sealed record SendDirectMessageRequest(
		string? Content,
		long? ReplyToId,
		string? Nonce);

	private sealed record ErrorBody(string Error);
}
