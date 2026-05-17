using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Features.Categories.Common;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Categories.CreateCategory;

internal sealed class CreateCategoryHandler(
	IGuildRepository guilds,
	IChannelCategoryRepository categories,
	IIdGenerator ids,
	IClock clock,
	ICurrentUser currentUser)
	: ICommandHandler<CreateCategoryCommand, Result<CategoryResponse>>
{
	public async Task<Result<CategoryResponse>> HandleAsync(
		CreateCategoryCommand command,
		CancellationToken cancellationToken = default)
	{
		var guild = await guilds.GetByIdWithMembershipAsync(command.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		var isMember = guild.Members.Any(m => m.UserId == currentUser.Id);
		if (!isMember)
			return GuildFailures.NotAMember;

		var effectiveMask = PermissionResolver.Resolve(
			currentUser.Id,
			guild.OwnerId,
			guild.Roles,
			guild.MemberRoles);

		if (!PermissionResolver.HasPermission(effectiveMask, Permission.ManageChannels))
			return GuildFailures.MissingPermission;

		int position;
		if (command.Position is int requested)
		{
			position = requested;
		}
		else
		{
			var max = await categories.GetMaxPositionAsync(command.GuildId, cancellationToken);
			position = (max ?? -1) + 1;
		}

		var categoryResult = ChannelCategory.Create(
			id: ids.NextId(),
			guildId: command.GuildId,
			name: command.Name,
			position: position,
			now: clock.UtcNow);

		if (categoryResult.IsFailure)
			return categoryResult.Error;

		await categories.AddAsync(categoryResult.Value, cancellationToken);
		await categories.SaveChangesAsync(cancellationToken);

		return CategoryResponse.From(categoryResult.Value);
	}
}
