namespace Chat.Application.Features.Channels.Common;

/// <summary>
/// wire shape for both PUT /channels/{id}/read and one entry of
/// GET /channels/read-states. LastReadMessageId is null when the channel has
/// never been read.
/// </summary>
public sealed record ChannelReadStateResponse(string ChannelId, string? LastReadMessageId, int UnreadCount);
