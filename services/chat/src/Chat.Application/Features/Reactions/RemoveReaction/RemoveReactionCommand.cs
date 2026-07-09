using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.Channels.Common;
using Chat.Domain.Results;

namespace Chat.Application.Features.Reactions.RemoveReaction;

public sealed record RemoveReactionCommand(long MessageId, string Emoji) : ICommand<Result<ReactionResponse>>;
