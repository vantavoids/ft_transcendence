using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Users.ExportUserData;
using Guild.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Guild.Presentation.Endpoints;

/// <summary>
/// internal GDPR data-export lookup: returns the user's Guild-owned data as JSON,
/// consumed by the User Service's public data-export aggregator. mounted under
/// <c>/internal</c> by <c>Program.cs</c>, so unreachable from outside the docker
/// network (the API Gateway only forwards <c>/api/{service}/vN/...</c>).
/// </summary>
public static class UserDataExportEndpoint
{
	public static void MapInternalRoutes(IEndpointRouteBuilder endpoints)
	{
		endpoints.MapGet("/users/{userId:long}/data-export", GetAsync);
	}

	private static async Task<Results<
		Ok<UserDataExportResponse>,
		BadRequest<ErrorBody>>>
	GetAsync(
		long userId,
		IQueryHandler<ExportUserDataQuery, Result<UserDataExportResponse>> handler,
		CancellationToken cancellationToken)
	{
		if (userId <= 0)
			return TypedResults.BadRequest(new ErrorBody("user_id must be a positive snowflake."));

		// a well-formed request always succeeds; an unknown user just exports empty
		var result = await handler.HandleAsync(new ExportUserDataQuery(userId), cancellationToken);
		return TypedResults.Ok(result.Value);
	}
}
