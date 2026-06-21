using Auth.Application.Abstractions.Messaging;
using Auth.Domain.Results;

namespace Auth.Application.Features.GetMe;

public sealed record GetMeQuery(long UserId) : IQuery<Result<GetMeResponse>>;
