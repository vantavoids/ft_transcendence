using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Features.Guilds.Common;
using Guild.Domain.Results;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.Application.Features.Guilds.CreateGuild;

internal sealed class CreateGuildHandler(
	IGuildRepository repository,
	IIdGenerator ids,
	IClock clock,
	ICurrentUser currentUser)
	: ICommandHandler<CreateGuildCommand, Result<GuildDto>>
{
	public async Task<Result<GuildDto>> HandleAsync(
		CreateGuildCommand command,
		CancellationToken cancellationToken = default)
	{
		var now = clock.UtcNow;
		var guildId = ids.NextId();
		var everyoneRoleId = ids.NextId();
		var adminRoleId = ids.NextId();

		var guildResult = GuildEntity.Create(
			id: guildId,
			name: command.Name,
			description: command.Description,
			iconUrl: command.IconUrl,
			bannerUrl: command.BannerUrl,
			ownerId: currentUser.Id,
			everyoneRoleId: everyoneRoleId,
			adminRoleId: adminRoleId,
			now: now);

		if (guildResult.IsFailure)
			return guildResult.Error;

		await repository.AddAsync(guildResult.Value, cancellationToken);
		await repository.SaveChangesAsync(cancellationToken);

		return GuildDto.FromEntity(guildResult.Value);
	}
}
