using Carter;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.Attachments.Common;
using Chat.Application.Features.Attachments.DownloadAttachment;
using Chat.Application.Features.Attachments.UploadAttachment;
using Chat.Domain.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Net.Http.Headers;

namespace Chat.Presentation.Endpoints;

public sealed class AttachmentsEndpoint : ICarterModule
{
	// browsers render these natively; everything else is forced to download so a
	// spoofed/active MIME (text/html, image/svg+xml, ...) can't execute on our
	// origin. the stored MIME is client-supplied at upload time, so it's untrusted
	private static readonly HashSet<string> InlineRenderableTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		"image/png", "image/jpeg", "image/gif", "image/webp", "image/bmp",
		"audio/mpeg", "audio/ogg", "audio/wav",
		"video/mp4", "video/webm",
		"application/pdf",
	};

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

		// never let the browser sniff a different (executable) type out of the bytes:
		// the stored MIME is attacker-controllable, so disable content sniffing
		http.Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";

		// render known-safe media inline (the contract intent for images); force a
		// download for anything else so an HTML/SVG payload can't run on our origin
		var disposition = InlineRenderableTypes.Contains(download.MimeType) ? "inline" : "attachment";

		// SetHttpFileName safely encodes the user-supplied filename into both the
		// ascii `filename` fallback and the RFC 5987 `filename*` form, so a quote or
		// non-ascii char in the name can't break out of the header
		var contentDisposition = new ContentDispositionHeaderValue(disposition);
		contentDisposition.SetHttpFileName(download.Filename);
		http.Response.Headers.ContentDisposition = contentDisposition.ToString();

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
