using Chat.Application.Abstractions.Messaging;
using Chat.Application.Abstractions.Persistence;
using Chat.Domain.Results;

namespace Chat.Application.Features.Users.ExportUserData;

internal sealed class ExportUserDataHandler(IDataExportRepository repository)
	: IQueryHandler<ExportUserDataQuery, Result<UserDataExportResponse>>
{
	public async Task<Result<UserDataExportResponse>> HandleAsync(
		ExportUserDataQuery query,
		CancellationToken cancellationToken = default)
	{
		var data = await repository.GetUserDataExportAsync(query.UserId, cancellationToken);

		return new UserDataExportResponse(
			query.UserId.ToString(),
			data.ChannelMessages.Select(ChannelMessageDto.From).ToList(),
			data.DirectMessages.Select(DirectMessageDto.From).ToList());
	}
}
