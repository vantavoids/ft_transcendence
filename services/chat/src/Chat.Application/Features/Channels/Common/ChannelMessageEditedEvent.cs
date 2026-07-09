using Chat.Domain.Messages;

namespace Chat.Application.Features.Channels.Common;

public sealed record ChannelMessageEditedEvent(string Id, string ChannelId, string? Content, DateTimeOffset EditedAt)
{
	public static ChannelMessageEditedEvent From(Message m) => new(
		Id: m.Id.ToString(),
		ChannelId: m.ContainerId.ToString(),
		Content: m.Content,
		EditedAt: m.EditedAt!.Value);
}
