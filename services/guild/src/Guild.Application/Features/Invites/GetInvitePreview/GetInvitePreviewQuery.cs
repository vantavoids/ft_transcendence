using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Invites.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Invites.GetInvitePreview;

public sealed record GetInvitePreviewQuery(string Code) : IQuery<Result<InvitePreviewDto>>;
