using Guild.Domain.Results;

namespace Guild.Domain.Guild;

public sealed class GuildInvite
{
	public const int MaxCodeLen = 16;

	// EF Core constructor
	private GuildInvite() { }

	private GuildInvite(
		string code,
		long guildId,
		long createdBy,
		int? maxUses,
		DateTimeOffset? expiresAt,
		DateTimeOffset createdAt)
	{
		Code = code;
		GuildId = guildId;
		CreatedBy = createdBy;
		MaxUses = maxUses;
		Uses = 0;
		IsRevoked = false;
		ExpiresAt = expiresAt;
		CreatedAt = createdAt;
	}

	public string Code { get; private set; } = string.Empty;
	public long GuildId { get; private set; }
	public long CreatedBy { get; private set; }
	public int? MaxUses { get; private set; }
	public int Uses { get; private set; }
	public bool IsRevoked { get; private set; }
	public DateTimeOffset? ExpiresAt { get; private set; }
	public DateTimeOffset CreatedAt { get; private set; }

	public static Result<GuildInvite> Create(
		string code,
		long guildId,
		long createdBy,
		int? maxUses,
		DateTimeOffset? expiresAt,
		DateTimeOffset now)
	{
		if (string.IsNullOrEmpty(code) || code.Length > MaxCodeLen)
			return GuildFailures.InviteCodeInvalid;
		if (guildId <= 0 || createdBy <= 0)
			return GuildFailures.InviteCodeInvalid;
		if (maxUses is <= 0)
			return GuildFailures.InviteMaxUsesInvalid;
		if (expiresAt is { } exp && exp <= now)
			return GuildFailures.InviteExpiresInPast;

		return new GuildInvite(code, guildId, createdBy, maxUses, expiresAt, now);
	}

	/// <summary>
	/// validates that the invite is still usable at <paramref name="now"/> and
	/// increments <see cref="Uses"/>. callers must persist the invite afterwards
	/// </summary>
	public Result Consume(DateTimeOffset now)
	{
		if (IsRevoked)
			return GuildFailures.InviteUnusable;
		if (ExpiresAt is { } exp && exp <= now)
			return GuildFailures.InviteUnusable;
		if (MaxUses is { } cap && Uses >= cap)
			return GuildFailures.InviteUnusable;

		Uses += 1;
		return Result.Ok();
	}

	public Result Revoke()
	{
		if (IsRevoked)
			return GuildFailures.InviteAlreadyRevoked;
		IsRevoked = true;
		return Result.Ok();
	}

	public bool IsActive(DateTimeOffset now)
	{
		if (IsRevoked) return false;
		if (ExpiresAt is { } exp && exp <= now) return false;
		if (MaxUses is { } cap && Uses >= cap) return false;
		return true;
	}
}
