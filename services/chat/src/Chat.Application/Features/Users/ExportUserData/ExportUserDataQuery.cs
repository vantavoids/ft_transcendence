using Chat.Application.Abstractions.Messaging;
using Chat.Application.Abstractions.Persistence;
using Chat.Domain.Results;

namespace Chat.Application.Features.Users.ExportUserData;

public sealed record ExportUserDataQuery(long UserId) : IQuery<Result<UserDataExportResponse>>;

/// <summary>
/// the user's Chat-owned data for a GDPR export: the messages they authored, in
/// channels and DMs. ids are raw - Chat doesn't own channel/user names (Guild and
/// User do), so the User aggregator resolves them when stitching the bundle.
/// </summary>
public sealed record UserDataExportResponse(
	string UserId,
	IReadOnlyList<ChannelMessageDto> ChannelMessages,
	IReadOnlyList<DirectMessageDto> DirectMessages);

public sealed record ChannelMessageDto(
	string ChannelId,
	string MessageId,
	string Content,
	DateTimeOffset CreatedAt,
	DateTimeOffset? EditedAt)
{
	public static ChannelMessageDto From(ExportedChannelMessage m) =>
		new(m.ChannelId.ToString(), m.MessageId.ToString(), m.Content, m.CreatedAt, m.EditedAt);
}

public sealed record DirectMessageDto(
	string ConversationId,
	string PartnerId,
	string MessageId,
	string Content,
	DateTimeOffset CreatedAt,
	DateTimeOffset? EditedAt)
{
	public static DirectMessageDto From(ExportedDirectMessage m) =>
		new(m.ConversationId.ToString(), m.PartnerId.ToString(), m.MessageId.ToString(), m.Content, m.CreatedAt, m.EditedAt);
}
