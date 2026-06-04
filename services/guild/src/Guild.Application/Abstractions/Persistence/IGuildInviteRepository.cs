using Guild.Domain.Guild;

namespace Guild.Application.Abstractions.Persistence;

public interface IGuildInviteRepository
{
	Task<GuildInvite?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

	/// <summary>returns non-revoked invites for the guild. expired / exhausted entries are still returned so callers can decide whether to display them; <see cref="GuildInvite.IsActive"/> filters them out</summary>
	Task<IReadOnlyList<GuildInvite>> ListByGuildAsync(long guildId, CancellationToken cancellationToken = default);

	Task AddAsync(GuildInvite invite, CancellationToken cancellationToken = default);
	void Update(GuildInvite invite);

	Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
