using Auth.Application.Abstractions.Messaging;
using Auth.Application.Abstractions.Persistence;
using Auth.Domain.AuthUser;
using Auth.Domain.Results;

namespace Auth.Application.Features.GetMe;

internal sealed class GetMeHandler(IAuthUserRepository userRepository)
    : IQueryHandler<GetMeQuery, Result<GetMeResponse>>
{
    public async Task<Result<GetMeResponse>> HandleAsync(
        GetMeQuery query,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(query.UserId, cancellationToken);

        if (user is null || user.IsDeleted)
            return AuthFailures.InvalidAccessToken;

        var oauthProviders = user.OAuthIdentity is not null
            ? [user.OAuthIdentity.Provider.ToSlug()]
            : Array.Empty<string>();

        return new GetMeResponse(
            Id: user.Id.ToString(),
            Email: user.Email?.Value,
            EmailVerified: user.Email?.IsVerified ?? false,
            OAuthProviders: oauthProviders,
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt);
    }
}
