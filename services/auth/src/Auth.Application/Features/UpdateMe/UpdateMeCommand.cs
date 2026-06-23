using Auth.Application.Abstractions.Messaging;
using Auth.Application.Features.GetMe;
using Auth.Domain.Results;

namespace Auth.Application.Features.UpdateMe;

public sealed record UpdateMeCommand(
    long UserId,
    string? Email,
    string? CurrentPassword,
    string? NewPassword
) : ICommand<Result<GetMeResponse>>;
