using Auth.Application.Abstractions.Messaging;
using Auth.Application.Features.ExportUserData;
using Auth.Domain.Results;
using Auth.Presentation.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Auth.Presentation.Endpoints;

/// <summary>
/// internal GDPR data-export lookup: returns the user's Auth-owned data as JSON,
/// mounted under <c>/internal</c> by <c>Program.cs</c>, so unreachable from outside
/// the docker network.
/// </summary>
public static class UserDataExportEndpoint
{
	public static void MapInternalRoutes(IEndpointRouteBuilder endpoints)
	{
		endpoints.MapGet("/users/{userId:long}/data-export", GetAsync);
	}

	private static async Task<Results<
		Ok<UserDataExportResponse>,
		BadRequest<ErrorResponse>,
		JsonHttpResult<ErrorResponse>>>
	GetAsync(
		long userId,
		IQueryHandler<ExportUserDataQuery, Result<UserDataExportResponse>> handler,
		CancellationToken cancellationToken)
	{
		if (userId <= 0)
			return TypedResults.BadRequest(new ErrorResponse("user_id must be a positive snowflake."));

		var result = await handler.HandleAsync(new ExportUserDataQuery(userId), cancellationToken);
		return result.Succeeded
			? TypedResults.Ok(result.Value)
			: TypedResults.Json(new ErrorResponse(result.Error.Message), statusCode: StatusCodes.Status500InternalServerError);
	}
}
