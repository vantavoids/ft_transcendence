using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Abstractions.Persistence;
using Chat.Application.Features.Messages.Common;
using Chat.Domain.Results;

namespace Chat.Application.Features.Messages.GetChannelMessages;

internal sealed class GetChannelMessagesHandler(
	ICurrentUser currentUser,
	IGuildClient guildClient,
	IMessageRepository repository,
	IClock clock)
	: IQueryHandler<GetChannelMessagesQuery, Result<IReadOnlyList<MessageResponse>>>
{
	private const long ReadMessages = 1L << 1;
	private const long Administrator = 1L << 8;

	public async Task<Result<IReadOnlyList<MessageResponse>>> HandleAsync(
		GetChannelMessagesQuery query,
		CancellationToken cancellationToken = default)
	{
		var userId = currentUser.UserId;
		var membership = await guildClient.GetMembershipAsync(query.ChannelId, userId, cancellationToken);

		if (membership is null)
			return MessageFailures.ChannelNotFound;

		if (!membership.IsMember)
			return MessageFailures.NotAMember;

		if ((membership.Permissions & (ReadMessages | Administrator)) == 0)
			return MessageFailures.MissingReadPermission;

		var beforeTime = query.BeforeTime ?? clock.UtcNow;
		var messages = await repository.GetChannelMessagesAsync(query.ChannelId, beforeTime, query.Limit, cancellationToken);

		return Result.Ok<IReadOnlyList<MessageResponse>>(
			messages.Select(m => MessageResponse.From(m, null)).ToList());
	}
}
