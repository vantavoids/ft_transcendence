using Cassandra;
using Chat.Application.Abstractions.Persistence;
using Chat.Domain.ReadStates;

namespace Chat.Persistence.Repositories;

/// <summary>
/// Monotonic-if-newer is a plain read-then-write rather than a lightweight
/// transaction: a genuine race only happens when the same user advances their
/// own cursor from two devices at nearly the same instant, and the worst
/// outcome is a momentarily stale badge that self-corrects on the next read -
/// not worth the complexity of a check-then-insert-if-not-exists /
/// update-if-newer LWT dance for a per-user cursor.
/// </summary>
internal sealed class ReadStateRepository(ISession session, ReadStateStatements statements) : IReadStateRepository
{
	public async Task<ReadState> UpsertIfNewerAsync(
		long userId, long containerId, bool isDm, long messageId, DateTimeOffset readAt, CancellationToken ct)
	{
		var selectStmt = await (isDm ? statements.SelectDmReadState : statements.SelectChannelReadState).Value;
		var row = (await session.ExecuteAsync(selectStmt.Bind(userId, containerId))).FirstOrDefault();

		var currentId = row?.GetValue<long?>("last_read_message_id");
		if (currentId is not null && messageId <= currentId)
			return new ReadState(
				containerId, isDm, currentId, new DateTimeOffset(row!.GetValue<DateTime>("last_read_at"), TimeSpan.Zero));

		var upsertStmt = await (isDm ? statements.UpsertDmReadState : statements.UpsertChannelReadState).Value;
		await session.ExecuteAsync(upsertStmt.Bind(userId, containerId, messageId, readAt.UtcDateTime));

		return new ReadState(containerId, isDm, messageId, readAt);
	}

	/* # Channels methods # */

	public async Task<IReadOnlyList<ReadState>> GetChannelReadStatesForUserAsync(long userId, CancellationToken ct)
	{
		var stmt = await statements.SelectChannelReadStatesForUser.Value;
		var rows = await session.ExecuteAsync(stmt.Bind(userId));

		return rows.Select(row => new ReadState(
			row.GetValue<long>("channel_id"),
			false,
			row.GetValue<long?>("last_read_message_id"),
			row.GetValue<DateTime?>("last_read_at") is { } at ? new DateTimeOffset(at, TimeSpan.Zero) : null))
			.ToList();
	}

	public async Task<int> CountChannelMessagesAfterAsync(long channelId, DateTimeOffset after, CancellationToken ct)
	{
		var stmt = await statements.CountChannelMessagesAfter.Value;
		var row = (await session.ExecuteAsync(stmt.Bind(channelId, after.UtcDateTime))).First();
		return (int)row.GetValue<long>("count");
	}


	/* # Direct Messages methods # */

	public async Task ResetDmUnreadCountAsync(long userId, long partnerId, CancellationToken ct)
	{
		var stmt = await statements.ResetDmUnreadCount.Value;
		await session.ExecuteAsync(stmt.Bind(userId, partnerId));
	}

	public async Task IncrementDmUnreadCountAsync(long userId, long partnerId, CancellationToken ct)
	{
		var stmt = await statements.IncrementDmUnreadCount.Value;
		await session.ExecuteAsync(stmt.Bind(userId, partnerId));
	}

	public async Task DeleteAllForUserAsync(long userId, CancellationToken ct)
	{
		var channelStmt = await statements.DeleteChannelReadStatesForUser.Value;
		var dmStmt = await statements.DeleteDmReadStatesForUser.Value;
		var unreadStmt = await statements.DeleteDmUnreadCountsForUser.Value;

		await Task.WhenAll(
			session.ExecuteAsync(channelStmt.Bind(userId)),
			session.ExecuteAsync(dmStmt.Bind(userId)),
			session.ExecuteAsync(unreadStmt.Bind(userId)));
	}
}
