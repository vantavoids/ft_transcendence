using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.Channels.Common;
using Chat.Domain.Results;

namespace Chat.Application.Features.Reactions.AddReaction;

public sealed record AddReactionCommand(long MessageId, string Emoji) : ICommand<Result<ReactionResponse>>;
