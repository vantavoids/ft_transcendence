using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.Attachments.Common;
using Chat.Domain.Results;

namespace Chat.Application.Features.Attachments.DownloadAttachment;

public sealed record DownloadAttachmentQuery(long Id, string Filename)
	: IQuery<Result<AttachmentDownload>>;
