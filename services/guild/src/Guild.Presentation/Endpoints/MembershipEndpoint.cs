using Carter;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Guilds.Common;
using Guild.Application.Features.Membership.Common;
using Guild.Application.Features.Membership.JoinByInviteCode;
using Guild.Application.Features.Membership.KickMember;
using Guild.Application.Features.Membership.LeaveGuild;
using Guild.Application.Features.Membership.ListMembers;
using Guild.Application.Features.Membership.UpdateNickname;
using Guild.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Guild.Presentation.Endpoints;

public sealed class MembershipEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup("/guilds/{id:long}");
		group.MapPost("/join", JoinAsync).ProducesGuildErrors().ProducesConflict();
		group.MapPost("/leave", LeaveAsync).ProducesGuildErrors();
		group.MapGet("/members", ListMembersAsync).ProducesGuildErrors();
		group.MapPatch("/members/{userId:long}", UpdateNicknameAsync).ProducesGuildErrors();
		group.MapDelete("/members/{userId:long}", KickAsync).ProducesGuildErrors();
	}

	private static async Task<Results<Ok<GuildDto>, JsonHttpResult<ErrorBody>>>
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

		// contract: on POST /guilds/{id}/join an invalid/expired/wrong-guild invite
		// is a 400 (the body carried a bad code), unlike POST /invites/{code}/join
		// where the same failures are 404. override the table's 404 default here.
		return EndpointResults.Problem(
			result.Error,
			("Guild.InviteNotFound", StatusCodes.Status400BadRequest),
			("Guild.InviteUnusable", StatusCodes.Status400BadRequest),
			("Guild.InviteGuildMismatch", StatusCodes.Status400BadRequest));
	}

	private static async Task<Results<NoContent, JsonHttpResult<ErrorBody>>>
	LeaveAsync(
		long id,
		ICommandHandler<LeaveGuildCommand, Result> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(new LeaveGuildCommand(id), cancellationToken);

		if (result.Succeeded)
			return TypedResults.NoContent();

		return EndpointResults.Problem(result.Error);
	}

	private static async Task<Results<Ok<IReadOnlyList<MemberResponse>>, JsonHttpResult<ErrorBody>>>
	ListMembersAsync(
		long id,
		string? after,
		int? limit,
		IQueryHandler<ListMembersQuery, Result<MemberListResponse>> handler,
		CancellationToken cancellationToken)
	{
		var (afterCursor, effectiveLimit, error) = Pagination.Parse(after, limit);
		if (error is not null)
			return error;

		var result = await handler.HandleAsync(
			new ListMembersQuery(id, afterCursor, effectiveLimit),
			cancellationToken);

		if (result.Succeeded)
			return TypedResults.Ok(result.Value.Items);

		return EndpointResults.Problem(result.Error);
	}

	private static async Task<Results<Ok<MemberResponse>, JsonHttpResult<ErrorBody>>>
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

		return EndpointResults.Problem(result.Error);
	}

	private static async Task<Results<NoContent, JsonHttpResult<ErrorBody>>>
	KickAsync(
		long id,
		long userId,
		ICommandHandler<KickMemberCommand, Result> handler,
		CancellationToken cancellationToken)
	{
		var result = await handler.HandleAsync(new KickMemberCommand(id, userId), cancellationToken);

		if (result.Succeeded)
			return TypedResults.NoContent();

		return EndpointResults.Problem(result.Error);
	}

	private sealed record JoinByPathRequest(string? InviteCode);
	private sealed record UpdateNicknameRequest(string? Nickname);
}
