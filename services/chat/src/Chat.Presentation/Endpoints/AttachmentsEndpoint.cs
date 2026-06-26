using Carter;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.Attachments.Common;
using Chat.Application.Features.Attachments.DownloadAttachment;
using Chat.Application.Features.Attachments.UploadAttachment;
using Chat.Domain.Results;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Chat.Presentation.Endpoints;

public sealed class AttachmentsEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup("/attachments");
		// form binding requires antiforgery validation by default; this API is
		// Bearer-authenticated and not browser-form driven, so opt out
		group.MapPost("/", UploadAsync).DisableAntiforgery();
		group.MapGet("/{id:long}/{filename}", DownloadAsync);
	}

	private static async Task<Results<
		Created<AttachmentResponse>,
		BadRequest<ErrorBody>,
		JsonHttpResult<ErrorBody>>>
	UploadAsync(
		IFormFile? file,
		ICommandHandler<UploadAttachmentCommand, Result<AttachmentResponse>> handler,
		CancellationToken cancellationToken)
	{
		if (file is null || file.Length == 0)
			return TypedResults.BadRequest(new ErrorBody("A non-empty 'file' field is required."));

		await using var stream = file.OpenReadStream();
		var result = await handler.HandleAsync(
			new UploadAttachmentCommand(stream, file.FileName, file.ContentType, file.Length),
			cancellationToken);

		return result.Succeeded
			? TypedResults.Created(result.Value.Url, result.Value)
			: MapUploadError(result.Error);
	}

	private static async Task<Results<
		FileStreamHttpResult,
		JsonHttpResult<ErrorBody>,
		NotFound<ErrorBody>>>
	DownloadAsync(
		long id,
		string filename,
		IQueryHandler<DownloadAttachmentQuery, Result<AttachmentDownload>> handler,
		HttpContext http,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(new DownloadAttachmentQuery(id, filename), cancellationToken);
		if (result.IsFailure)
			return MapDownloadError(result.Error);

		var download = result.Value;
		// stream inline (contract); TypedResults.Stream would force "attachment" if
		// we passed a download name, so set the disposition explicitly
		http.Response.Headers.ContentDisposition = $"inline; filename=\"{download.Filename}\"";
		return TypedResults.Stream(download.Content, download.MimeType);
	}

	private static Results<Created<AttachmentResponse>, BadRequest<ErrorBody>, JsonHttpResult<ErrorBody>>
		MapUploadError(Failure failure) => failure.Code switch
		{
			"Attachment.BlockedMimeType" => TypedResults.Json(
				new ErrorBody(failure.Message), statusCode: StatusCodes.Status415UnsupportedMediaType),
			_ => TypedResults.BadRequest(new ErrorBody(failure.Message)),
		};

	private static Results<FileStreamHttpResult, JsonHttpResult<ErrorBody>, NotFound<ErrorBody>>
		MapDownloadError(Failure failure) => failure.Code switch
		{
			"Attachment.NotAuthorized" => TypedResults.Json(
				new ErrorBody(failure.Message), statusCode: StatusCodes.Status403Forbidden),
			_ => TypedResults.NotFound(new ErrorBody(failure.Message)),
		};
}
