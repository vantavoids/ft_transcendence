namespace Chat.Domain.ReadStates;

/// <summary>
/// A user's last-read cursor for one channel or DM conversation.
/// <see cref="ContainerId"/> is the channel id or the partner's user id
/// depending on <see cref="IsDm"/> - mirrors <c>Message.ContainerId</c>'s
/// discriminator shape. <see cref="LastReadMessageId"/> and
/// <see cref="LastReadAt"/> are both <c>null</c> when the user has never
/// read the channel/conversation - there is no row yet.
/// </summary>
public sealed record ReadState(long ContainerId, bool IsDm, long? LastReadMessageId, DateTimeOffset? LastReadAt);
