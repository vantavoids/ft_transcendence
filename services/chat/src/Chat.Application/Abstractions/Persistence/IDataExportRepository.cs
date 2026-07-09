namespace Chat.Application.Abstractions.Persistence;

/// <summary>
/// read-only access for the GDPR data export: the user's own messages. Scylla is
/// partitioned by channel/conversation, not author, so we find the partitions the
/// user touched via the per-user index tables (channel_read_states,
/// user_conversations) and filter each to the user's own rows.
/// </summary>
public interface IDataExportRepository
{
	Task<ChatUserDataExport> GetUserDataExportAsync(long userId, CancellationToken ct);
}

public sealed record ChatUserDataExport(
	IReadOnlyList<ExportedChannelMessage> ChannelMessages,
	IReadOnlyList<ExportedDirectMessage> DirectMessages);

public sealed record ExportedChannelMessage(
	long ChannelId,
	long MessageId,
	string Content,
	DateTimeOffset CreatedAt,
	DateTimeOffset? EditedAt);

public sealed record ExportedDirectMessage(
	long ConversationId,
	long PartnerId,
	long MessageId,
	string Content,
	DateTimeOffset CreatedAt,
	DateTimeOffset? EditedAt);
