using Cassandra;
using Chat.Application.Abstractions.Persistence;

namespace Chat.Persistence.Repositories;

internal sealed class DataExportRepository(ISession session, DataExportStatements statements) : IDataExportRepository
{
	public async Task<ChatUserDataExport> GetUserDataExportAsync(long userId, CancellationToken ct)
	{
		var channelMessages = await CollectChannelMessagesAsync(userId);
		var directMessages = await CollectDirectMessagesAsync(userId);
		return new ChatUserDataExport(channelMessages, directMessages);
	}

	private async Task<List<ExportedChannelMessage>> CollectChannelMessagesAsync(long userId)
	{
		var channelsStmt = await statements.SelectUserChannels.Value;
		var channelRows = await session.ExecuteAsync(channelsStmt.Bind(userId));
		var channelIds = channelRows.Select(r => r.GetValue<long>("channel_id")).ToList();

		var messages = new List<ExportedChannelMessage>();
		if (channelIds.Count == 0)
			return messages;

		var msgStmt = await statements.SelectAuthoredChannelMessages.Value;
		foreach (var channelId in channelIds)
		{
			var rows = await session.ExecuteAsync(msgStmt.Bind(channelId, userId));
			foreach (var r in rows)
			{
				if (r.GetValue<bool>("is_deleted"))
					continue;
				messages.Add(new ExportedChannelMessage(
					channelId,
					r.GetValue<long>("id"),
					r.GetValue<string>("content"),
					r.GetValue<DateTimeOffset>("created_at"),
					r.GetValue<DateTimeOffset?>("edited_at")));
			}
		}
		return messages;
	}

	private async Task<List<ExportedDirectMessage>> CollectDirectMessagesAsync(long userId)
	{
		var convStmt = await statements.SelectUserConversations.Value;
		var convRows = await session.ExecuteAsync(convStmt.Bind(userId));
		var conversations = convRows
			.Select(r => (PartnerId: r.GetValue<long>("partner_id"), ConversationId: r.GetValue<long>("conversation_id")))
			.ToList();

		var messages = new List<ExportedDirectMessage>();
		if (conversations.Count == 0)
			return messages;

		var msgStmt = await statements.SelectAuthoredDmMessages.Value;
		foreach (var (partnerId, conversationId) in conversations)
		{
			var rows = await session.ExecuteAsync(msgStmt.Bind(conversationId, userId));
			foreach (var r in rows)
			{
				if (r.GetValue<bool>("is_deleted"))
					continue;
				messages.Add(new ExportedDirectMessage(
					conversationId,
					partnerId,
					r.GetValue<long>("id"),
					r.GetValue<string>("content"),
					r.GetValue<DateTimeOffset>("created_at"),
					r.GetValue<DateTimeOffset?>("edited_at")));
			}
		}
		return messages;
	}
}
