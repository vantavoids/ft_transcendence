using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.Attachments.Common;
using Chat.Domain.Results;

namespace Chat.Application.Features.Attachments.UploadAttachment;

/// <summary>
/// upload a draft attachment. <see cref="Content"/> is the open request stream; the
/// handler reads it straight into object storage without buffering the whole file
/// </summary>
public sealed record UploadAttachmentCommand(
	Stream Content,
	string FileName,
	string? ContentType,
	long Length)
	: ICommand<Result<AttachmentResponse>>;
