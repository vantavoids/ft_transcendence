using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Features.Categories.Common;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Categories.UpdateCategory;

internal sealed class UpdateCategoryHandler(
	IGuildRepository guilds,
	IChannelCategoryRepository categories,
	IClock clock,
	ICurrentUser currentUser)
	: ICommandHandler<UpdateCategoryCommand, Result<CategoryResponse>>
{
	public async Task<Result<CategoryResponse>> HandleAsync(
		UpdateCategoryCommand command,
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

		var category = await categories.GetByIdAsync(command.GuildId, command.CategoryId, cancellationToken);
		if (category is null)
			return GuildFailures.CategoryNotFound;

		var now = clock.UtcNow;

		if (command.Name is not null)
		{
			var renameResult = category.Rename(command.Name, now);
			if (renameResult.IsFailure)
				return renameResult.Error;
		}

		if (command.Position is int newPosition)
		{
			var repositionResult = category.Reposition(newPosition, now);
			if (repositionResult.IsFailure)
				return repositionResult.Error;
		}

		categories.Update(category);
		await categories.SaveChangesAsync(cancellationToken);

		return CategoryResponse.From(category);
	}
}
