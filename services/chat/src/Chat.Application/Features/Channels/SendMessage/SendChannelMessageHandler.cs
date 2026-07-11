using System.Text.RegularExpressions;
using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Abstractions.Persistence;
using Chat.Application.Contracts;
using Chat.Application.Features.Channels.Common;
using Chat.Application.Features.Messages.SendMessage;
using Chat.Domain.Messages;
using Chat.Domain.Reactions;
using Chat.Domain.Results;

namespace Chat.Application.Features.Channels.SendMessage;

internal sealed class SendChannelMessageHandler(
	ICurrentUser currentUser,
	IMessageRepository repository,
	IAttachmentRepository attachmentRepository,
	ISnowflakeIdGenerator ids,
	IClock clock,
	IGuildClient guildClient,
	IEventBus eventBus,
	IChannelBroadcaster broadcaster,
	IReactionRepository reactionRepository)
	: SendMessageHandlerBase<SendChannelMessageCommand, ChannelMessageResponse, long>(currentUser, repository, attachmentRepository, ids, clock)
{
	// permission bits mirror the Guild Service domain so this stays a pure
	// bitmask check; see services/guild/src/Guild.Domain/Guild/Permission.cs
	// for the source of truth on permission numbering
	private const long SendMessages = 1L << 0;
	private const long Administrator = 1L << 8;

	// TContext = GuildId, resolved here and needed again for ChatMessageSent
	protected override async Task<Result<long>> PrecheckAsync(SendChannelMessageCommand command, CancellationToken ct)
	{
		var membership = await guildClient.GetMembershipAsync(command.ChannelId, AuthorId, ct);
		if (membership is null)
			return MessageFailures.ChannelNotFound;

		if (!membership.IsMember)
			return MessageFailures.NotAMember;

		if ((membership.Permissions & (SendMessages | Administrator)) == 0)
			return MessageFailures.MissingSendPermission;

		return membership.GuildId;
	}

	protected override Task<long?> FindNonceAsync(SendChannelMessageCommand command, string nonce, CancellationToken ct) =>
		Repository.FindChannelNonceAsync(AuthorId, command.ChannelId, nonce, ct);

	protected override Task<long?> FindExistingContainerIdAsync(SendChannelMessageCommand command, CancellationToken ct) =>
		Task.FromResult<long?>(command.ChannelId);

	protected override Task<long> ResolveContainerIdAsync(SendChannelMessageCommand command, CancellationToken ct) =>
		Task.FromResult(command.ChannelId);

	// on a nonce replay, the existing message may have accrued reactions since
	// the original send, so hydrate them for real instead of always returning []
	protected override async Task<IReadOnlyList<ReactionSummary>?> ResolveReactionsAsync(Message existing, CancellationToken ct) =>
		await reactionRepository.GetMessageReactionsAsync(existing.ContainerId, existing.Id, AuthorId, ct);

	protected override Result<Message> CreateMessage(
		SendChannelMessageCommand command, long containerId, long messageId, bool hasAttachments, DateTimeOffset now) =>
		Message.CreateForChannel(
			id: messageId,
			channelId: containerId,
			authorId: AuthorId,
			content: command.Content,
			replyToId: command.ReplyToId,
			now: now,
			hasAttachments: hasAttachments);

	protected override async Task PublishAndNotifyAsync(
		SendChannelMessageCommand command, long guildId, Message message, ChannelMessageResponse response, CancellationToken ct)
	{
		await eventBus.PublishAsync(
			new ChatMessageSent(
				ChannelId: command.ChannelId,
				GuildId: guildId,
				AuthorId: AuthorId,
				MessageId: message.Id,
				Content: message.Content ?? string.Empty,
				Mentions: ParseMentions(message.Content, AuthorId)),
			ct);

		await broadcaster.BroadcastMessageAsync(command.ChannelId, response, ct);
	}

	// matches the Discord-style mention tokens the frontend inserts on @-select
	// (<@123>). extract the snowflake ids, dedup, and drop the author so a
	// self-mention never notifies. channel membership is not re-checked here:
	// the Notification Service already suppresses blocked pairs.
	private static readonly Regex MentionPattern = new(@"<@(\d{1,20})>", RegexOptions.Compiled);

	private static long[] ParseMentions(string? content, long authorId)
	{
		if (string.IsNullOrEmpty(content))
			return [];

		var ids = new HashSet<long>();
		foreach (Match match in MentionPattern.Matches(content))
		{
			if (long.TryParse(match.Groups[1].Value, out var id) && id != authorId)
				ids.Add(id);
		}

		return ids.Count == 0 ? [] : [.. ids];
	}
}
