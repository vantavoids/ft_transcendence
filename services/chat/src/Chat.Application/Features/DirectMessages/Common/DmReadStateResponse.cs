namespace Chat.Application.Features.DirectMessages.Common;

/// <summary>wire shape for PUT /dms/{user_id}/read.</summary>
public sealed record DmReadStateResponse(string PartnerId, string? LastReadMessageId, int UnreadCount);
