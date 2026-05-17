using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Categories.DeleteCategory;

internal sealed class DeleteCategoryHandler(
	IGuildRepository guilds,
	IChannelCategoryRepository categories,
	ICurrentUser currentUser)
	: ICommandHandler<DeleteCategoryCommand, Result>
{
	public async Task<Result> HandleAsync(
		DeleteCategoryCommand command,
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

		categories.Remove(category);
		await categories.SaveChangesAsync(cancellationToken);

		return Result.Ok();
	}
}
