using Carter;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Guilds.Common;
using Guild.Application.Features.Membership.Common;
using Guild.Application.Features.Membership.JoinByInviteCode;
using Guild.Application.Features.Membership.LeaveGuild;
using Guild.Application.Features.Membership.ListMembers;
using Guild.Application.Features.Membership.UpdateNickname;
using Guild.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Guild.Presentation.Endpoints;

public sealed class MembershipEndpoint : ICarterModule
{
	private const int DefaultListLimit = 50;
	private const int MaxListLimit = 100;

	public void AddRoutes(IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup("/guilds/{id:long}");
		group.MapPost("/join", JoinAsync);
		group.MapPost("/leave", LeaveAsync);
		group.MapGet("/members", ListMembersAsync);
		group.MapPatch("/members/{userId:long}", UpdateNicknameAsync);
	}

	private static async Task<Results<
		Ok<GuildDto>,
		BadRequest<ErrorBody>,
		NotFound<ErrorBody>,
		Conflict<ErrorBody>,
		JsonHttpResult<ErrorBody>>>
	JoinAsync(
		long id,
		JoinByPathRequest request,
		ICommandHandler<JoinByInviteCodeCommand, Result<GuildDto>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new JoinByInviteCodeCommand(request.InviteCode, ExpectedGuildId: id),
			cancellationToken);

		if (result.Succeeded)
			return TypedResults.Ok(result.Value);

		return result.Error.Code switch
		{
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(result.Error.Message)),
			"Guild.AlreadyMember" => TypedResults.Conflict(new ErrorBody(result.Error.Message)),
			_ => TypedResults.BadRequest(new ErrorBody(result.Error.Message)),
		};
	}

	private static async Task<Results<
		NoContent,
		BadRequest<ErrorBody>,
		NotFound<ErrorBody>,
		JsonHttpResult<ErrorBody>>>
	LeaveAsync(
		long id,
		ICommandHandler<LeaveGuildCommand, Result> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(new LeaveGuildCommand(id), cancellationToken);

		if (result.Succeeded)
			return TypedResults.NoContent();

		return result.Error.Code switch
		{
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(result.Error.Message)),
			"Guild.OwnerCannotLeave" => TypedResults.BadRequest(new ErrorBody(result.Error.Message)),
			"Guild.NotAMember" => TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status403Forbidden),
			_ => TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status403Forbidden),
		};
	}

	private static async Task<Results<
		Ok<IReadOnlyList<MemberResponse>>,
		BadRequest<ErrorBody>,
		NotFound<ErrorBody>,
		JsonHttpResult<ErrorBody>>>
	ListMembersAsync(
		long id,
		string? after,
		int? limit,
		IQueryHandler<ListMembersQuery, Result<MemberListResponse>> handler,
		CancellationToken cancellationToken)
	{
		long? afterCursor = null;
		if (after is not null)
		{
			if (!long.TryParse(after, out var parsed) || parsed <= 0)
				return TypedResults.BadRequest(new ErrorBody("after must be a positive snowflake."));
			afterCursor = parsed;
		}

		var effectiveLimit = limit ?? DefaultListLimit;
		if (effectiveLimit <= 0 || effectiveLimit > MaxListLimit)
			return TypedResults.BadRequest(new ErrorBody($"limit must be between 1 and {MaxListLimit}."));

		var result = await handler.HandleAsync(
			new ListMembersQuery(id, afterCursor, effectiveLimit),
			cancellationToken);

		if (result.Succeeded)
			return TypedResults.Ok(result.Value.Items);

		return result.Error.Code switch
		{
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(result.Error.Message)),
			"Guild.NotAMember" => TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status403Forbidden),
			_ => TypedResults.BadRequest(new ErrorBody(result.Error.Message)),
		};
	}

	private static async Task<Results<
		Ok<MemberResponse>,
		BadRequest<ErrorBody>,
		NotFound<ErrorBody>,
		JsonHttpResult<ErrorBody>>>
	UpdateNicknameAsync(
		long id,
		long userId,
		UpdateNicknameRequest request,
		ICommandHandler<UpdateNicknameCommand, Result<MemberResponse>> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(
			new UpdateNicknameCommand(id, userId, request.Nickname),
			cancellationToken);

		if (result.Succeeded)
			return TypedResults.Ok(result.Value);

		return result.Error.Code switch
		{
			"Guild.GuildNotFound" => TypedResults.NotFound(new ErrorBody(result.Error.Message)),
			"Guild.TargetNotAMember" => TypedResults.NotFound(new ErrorBody(result.Error.Message)),
			"Guild.NotAMember" => TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.MissingPermission" => TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.RoleHierarchyBlocked" => TypedResults.Json(new ErrorBody(result.Error.Message), statusCode: StatusCodes.Status403Forbidden),
			"Guild.NicknameTooLong" => TypedResults.BadRequest(new ErrorBody(result.Error.Message)),
			"Guild.NicknameInvalid" => TypedResults.BadRequest(new ErrorBody(result.Error.Message)),
			_ => TypedResults.BadRequest(new ErrorBody(result.Error.Message)),
		};
	}

	private sealed record JoinByPathRequest(string? InviteCode);
	private sealed record UpdateNicknameRequest(string? Nickname);
}
