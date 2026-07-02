using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Domain.Results;

namespace Guild.Application.Features.Users.ExportUserData;

internal sealed class ExportUserDataHandler(IGuildRepository guilds)
	: IQueryHandler<ExportUserDataQuery, Result<UserDataExportResponse>>
{
	public async Task<Result<UserDataExportResponse>> HandleAsync(
		ExportUserDataQuery query,
		CancellationToken cancellationToken = default)
	{
		var data = await guilds.GetUserDataExportAsync(query.UserId, cancellationToken);

		return new UserDataExportResponse(
			query.UserId.ToString(),
			data.OwnedGuilds.Select(OwnedGuildDto.From).ToList(),
			data.Memberships.Select(MembershipDto.From).ToList());
	}
}
