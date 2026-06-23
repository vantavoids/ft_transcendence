using Auth.Application.Abstractions.Messaging;
using Auth.Domain.Results;

namespace Auth.Application.Features.DeleteMe;

public sealed record DeleteMeCommand(long UserId) : ICommand<Result>;
