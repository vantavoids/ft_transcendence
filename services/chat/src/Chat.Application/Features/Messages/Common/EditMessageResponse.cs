using Chat.Domain.Messages;

namespace Chat.Application.Features.Messages.Common;

public sealed record EditMessageResponse(string Id, string? Content, DateTimeOffset EditedAt)
{
	public static EditMessageResponse From(Message m) => new(
		Id: m.Id.ToString(),
		Content: m.Content,
		EditedAt: m.EditedAt!.Value);
}
