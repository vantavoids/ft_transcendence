using Cassandra;
using Chat.Application.Abstractions.Persistence;
using Chat.Domain.Reactions;

namespace Chat.Persistence.Repositories;

internal sealed class ReactionRepository(ISession session, ReactionStatements statements) : IReactionRepository
{
	public async Task<bool> AddAsync(long channelId, long messageId, long userId, string emoji, DateTimeOffset now, CancellationToken ct)
	{
		var insert = await statements.InsertReactionIfNotExists.Value;
		var row = (await session.ExecuteAsync(insert.Bind(channelId, messageId, emoji, userId, now.UtcDateTime))).First();

		var applied = row.GetValue<bool>("[applied]");
		if (!applied)
			return false;

		var increment = await statements.IncrementCount.Value;
		await session.ExecuteAsync(increment.Bind(channelId, messageId, emoji));
		return true;
	}

	public async Task<bool> RemoveAsync(long channelId, long messageId, long userId, string emoji, CancellationToken ct)
	{
		var delete = await statements.DeleteReactionIfExists.Value;
		var row = (await session.ExecuteAsync(delete.Bind(channelId, messageId, emoji, userId))).First();

		var applied = row.GetValue<bool>("[applied]");
		if (!applied)
			return false;

		var decrement = await statements.DecrementCount.Value;
		await session.ExecuteAsync(decrement.Bind(channelId, messageId, emoji));
		return true;
	}

	public async Task<long> GetCountAsync(long channelId, long messageId, string emoji, CancellationToken ct)
	{
		var stmt = await statements.SelectCount.Value;
		var row = (await session.ExecuteAsync(stmt.Bind(channelId, messageId, emoji))).FirstOrDefault();
		return row?.GetValue<long>("count") ?? 0;
	}

	public async Task<IReadOnlyList<ReactionSummary>> GetMessageReactionsAsync(
		long channelId, long messageId, long currentUserId, CancellationToken ct)
	{
		var countsStmt = await statements.SelectCountsForMessage.Value;
		var countRows = await session.ExecuteAsync(countsStmt.Bind(channelId, messageId));

		var mineStmt = await statements.SelectMineForMessage.Value;
		var mineRows = await session.ExecuteAsync(mineStmt.Bind(channelId, messageId, currentUserId));

		var mine = mineRows.Select(r => r.GetValue<string>("emoji")).ToHashSet();

		return countRows
			.Select(r =>
			{
				var emoji = r.GetValue<string>("emoji");
				return new ReactionSummary(emoji, r.GetValue<long>("count"), mine.Contains(emoji));
			})
			.ToList();
	}

	public async Task<ILookup<long, ReactionSummary>> GetMessagesReactionsAsync(
		long channelId, IReadOnlyList<long> messageIds, long currentUserId, CancellationToken ct)
	{
		if (messageIds.Count == 0)
			return Enumerable.Empty<ReactionSummary>().ToLookup(_ => 0L);

		var countsStmt = await statements.SelectCountsForMessages.Value;
		var countRows = await session.ExecuteAsync(countsStmt.Bind(channelId, messageIds.ToArray()));

		var mineStmt = await statements.SelectMineForMessages.Value;
		var mineRows = await session.ExecuteAsync(mineStmt.Bind(channelId, messageIds.ToArray(), currentUserId));

		var mine = mineRows
			.Select(r => (MessageId: r.GetValue<long>("message_id"), Emoji: r.GetValue<string>("emoji")))
			.ToHashSet();

		return countRows
			.Select(r =>
			{
				var messageId = r.GetValue<long>("message_id");
				var emoji = r.GetValue<string>("emoji");
				return (messageId, Reaction: new ReactionSummary(emoji, r.GetValue<long>("count"), mine.Contains((messageId, emoji))));
			})
			.ToLookup(x => x.messageId, x => x.Reaction);
	}
}
