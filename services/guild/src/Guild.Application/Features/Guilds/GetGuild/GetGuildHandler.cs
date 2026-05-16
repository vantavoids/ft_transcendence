using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Features.Guilds.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Guilds.GetGuild;

internal sealed class GetGuildHandler(
	IGuildRepository repository,
	ICurrentUser currentUser)
	: IQueryHandler<GetGuildQuery, Result<GuildDetailsDto>>
{
	public async Task<Result<GuildDetailsDto>> HandleAsync(
		GetGuildQuery query,
		CancellationToken cancellationToken = default)
	{
		var guild = await repository.GetByIdAsync(query.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		var isMember = await repository.IsMemberAsync(query.GuildId, currentUser.Id, cancellationToken);
		if (!isMember)
			return GuildFailures.NotAMember;

		var memberCount = await repository.CountMembersAsync(query.GuildId, cancellationToken);
		return GuildDetailsDto.FromEntity(guild, memberCount);
	}
}
