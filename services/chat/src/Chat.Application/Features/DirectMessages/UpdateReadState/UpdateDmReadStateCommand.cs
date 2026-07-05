using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.DirectMessages.Common;
using Chat.Domain.Results;

namespace Chat.Application.Features.DirectMessages.UpdateReadState;

public sealed record UpdateDmReadStateCommand(long PartnerId, long MessageId)
	: ICommand<Result<DmReadStateResponse>>;
