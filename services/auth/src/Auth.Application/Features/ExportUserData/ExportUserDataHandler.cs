using Auth.Application.Abstractions.Messaging;
using Auth.Application.Abstractions.Persistence;
using Auth.Domain.AuthUser;
using Auth.Domain.Results;

namespace Auth.Application.Features.ExportUserData;

internal sealed class ExportUserDataHandler(IAuthUserRepository users)
	: IQueryHandler<ExportUserDataQuery, Result<UserDataExportResponse>>
{
	public async Task<Result<UserDataExportResponse>> HandleAsync(
		ExportUserDataQuery query,
		CancellationToken cancellationToken = default)
	{
		var user = await users.GetByIdAsync(query.UserId, cancellationToken);

		if (user is null || user.IsDeleted)
			return new UserDataExportResponse(query.UserId.ToString(), null, null, null, null, null);

		return new UserDataExportResponse(
			UserId: user.Id.ToString(),
			Email: user.Email?.Value,
			EmailVerified: user.Email?.IsVerified,
			OAuthProvider: user.OAuthIdentity?.Provider.ToSlug(),
			OAuthId: user.OAuthIdentity?.Id,
			CreatedAt: user.CreatedAt);
	}
}
