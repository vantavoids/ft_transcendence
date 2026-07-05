using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Abstractions.Persistence;
using Chat.Application.Features.Channels.Common;
using Chat.Domain.Results;

namespace Chat.Application.Features.Channels.GetReadStates;

internal sealed class GetChannelReadStatesHandler(
	ICurrentUser currentUser,
	IGuildClient guildClient,
	IReadStateRepository readStates)
	: IQueryHandler<GetChannelReadStatesQuery, Result<IReadOnlyList<ChannelReadStateResponse>>>
{
	// no message ever predates the Unix epoch (Snowflake timestamps use a later
	// custom epoch); used as the "since always" lower bound for unread channels
	private static readonly DateTimeOffset SinceAlways = DateTimeOffset.UnixEpoch;

	public async Task<Result<IReadOnlyList<ChannelReadStateResponse>>> HandleAsync(
		GetChannelReadStatesQuery query, CancellationToken cancellationToken = default)
	{
		var userId = currentUser.UserId;

		var visibleChannelIds = await guildClient.GetVisibleChannelIdsAsync(userId, cancellationToken);
		var existingStates = await readStates.GetChannelReadStatesForUserAsync(userId, cancellationToken);
		var statesByChannel = existingStates.ToDictionary(s => s.ContainerId);

		var responses = new List<ChannelReadStateResponse>(visibleChannelIds.Count);
		foreach (var channelId in visibleChannelIds)
		{
			var since = statesByChannel.TryGetValue(channelId, out var state) ? state.LastReadAt : null;
			var unreadCount = await readStates.CountChannelMessagesAfterAsync(channelId, since ?? SinceAlways, cancellationToken);

			responses.Add(new ChannelReadStateResponse(
				channelId.ToString(), state?.LastReadMessageId?.ToString(), unreadCount));
		}

		return Result.Ok<IReadOnlyList<ChannelReadStateResponse>>(responses);
	}
}
