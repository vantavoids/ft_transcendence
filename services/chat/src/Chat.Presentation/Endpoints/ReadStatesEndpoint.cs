using Carter;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.Channels.Common;
using Chat.Application.Features.Channels.GetReadStates;
using Chat.Application.Features.Channels.UpdateReadState;
using Chat.Application.Features.DirectMessages.Common;
using Chat.Application.Features.DirectMessages.UpdateReadState;
using Chat.Domain.Results;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Chat.Presentation.Endpoints;

public sealed class ReadStatesEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		app.MapPut("/channels/{channelId:long}/read", UpdateChannelReadStateAsync).WithTags("Read States");
		app.MapGet("/channels/read-states", GetChannelReadStatesAsync).WithTags("Read States");
		app.MapPut("/dms/{userId:long}/read", UpdateDmReadStateAsync).WithTags("Read States");
	}

	private static async Task<Results<Ok<ChannelReadStateResponse>, BadRequest<ErrorBody>, JsonHttpResult<ErrorBody>, NotFound<ErrorBody>>>
	UpdateChannelReadStateAsync(
		long channelId,
		UpdateReadStateRequest request,
		ICommandHandler<UpdateChannelReadStateCommand, Result<ChannelReadStateResponse>> handler,
		CancellationToken cancellationToken)
	{
		if (request.MessageId is not ({ } messageId and > 0))
			return TypedResults.BadRequest(new ErrorBody("message_id is required and must be a positive snowflake."));

		var result = await handler.HandleAsync(new UpdateChannelReadStateCommand(channelId, messageId), cancellationToken);

		return result.Succeeded
			? TypedResults.Ok(result.Value)
			: MapChannelError(result.Error);
	}

	private static async Task<Results<Ok<IReadOnlyList<ChannelReadStateResponse>>, BadRequest<ErrorBody>>>
	GetChannelReadStatesAsync(
		IQueryHandler<GetChannelReadStatesQuery, Result<IReadOnlyList<ChannelReadStateResponse>>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(new GetChannelReadStatesQuery(), cancellationToken);

		return result.Succeeded
			? TypedResults.Ok(result.Value)
			: TypedResults.BadRequest(new ErrorBody(result.Error.Message));
	}

	private static async Task<Results<Ok<DmReadStateResponse>, BadRequest<ErrorBody>, NotFound<ErrorBody>>>
	UpdateDmReadStateAsync(
		long userId,
		UpdateReadStateRequest request,
		ICommandHandler<UpdateDmReadStateCommand, Result<DmReadStateResponse>> handler,
		CancellationToken cancellationToken)
	{
		if (request.MessageId is not ({ } messageId and > 0))
			return TypedResults.BadRequest(new ErrorBody("message_id is required and must be a positive snowflake."));

		var result = await handler.HandleAsync(new UpdateDmReadStateCommand(userId, messageId), cancellationToken);

		return result.Succeeded
			? TypedResults.Ok(result.Value)
			: MapDmError(result.Error);
	}

	private static Results<Ok<ChannelReadStateResponse>, BadRequest<ErrorBody>, JsonHttpResult<ErrorBody>, NotFound<ErrorBody>>
		MapChannelError(Failure failure) => failure.Code switch
		{
			"Message.ChannelNotFound" or
			"Message.NotFound" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			"Message.NotAMember" or
			"Message.MissingReadPermission" => TypedResults.Json(new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			_ => TypedResults.BadRequest(new ErrorBody(failure.Message)),
		};

	private static Results<Ok<DmReadStateResponse>, BadRequest<ErrorBody>, NotFound<ErrorBody>>
		MapDmError(Failure failure) => failure.Code switch
		{
			"Message.ConversationNotFound" or
			"Message.NotFound" => TypedResults.NotFound(new ErrorBody(failure.Message)),
			_ => TypedResults.BadRequest(new ErrorBody(failure.Message)),
		};

	private sealed record UpdateReadStateRequest(long? MessageId);
}
