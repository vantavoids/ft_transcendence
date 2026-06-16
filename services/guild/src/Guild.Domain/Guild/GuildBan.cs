using Guild.Domain.Results;

namespace Guild.Domain.Guild;

/// <summary>
/// per-guild ban row. composite PK is (GuildId, UserId), matching
/// <c>docs/schema/guild.sql:86-95</c>. owned by its own repository, not by the
/// Guild aggregate, so pre-emptive bans (target has never been a member) work
/// without loading membership state. <see cref="Reason"/> is free-form text
/// capped at <see cref="MaxReasonLen"/> characters by the factory
/// </summary>
public sealed class GuildBan
{
	public const int MaxReasonLen = 512;

	// EF Core constructor
	private GuildBan() { }

	private GuildBan(long guildId, long userId, long bannedBy, string? reason, DateTimeOffset bannedAt)
	{
		GuildId = guildId;
		UserId = userId;
		BannedBy = bannedBy;
		Reason = reason;
		BannedAt = bannedAt;
	}

	public long GuildId { get; private set; }
	public long UserId { get; private set; }
	public long BannedBy { get; private set; }
	public string? Reason { get; private set; }
	public DateTimeOffset BannedAt { get; private set; }

	public static Result<GuildBan> Create(
		long guildId,
		long userId,
		long bannedBy,
		string? reason,
		DateTimeOffset now)
	{
		if (reason is not null && reason.Length > MaxReasonLen)
			return GuildFailures.BanReasonTooLong;

		return new GuildBan(guildId, userId, bannedBy, reason, now);
	}
}
