using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace Chat.Presentation.Hubs;

// Inside a SignalR hub, IHttpContextAccessor.HttpContext is null (hub invocations
// don't run in an HTTP request scope), so ICurrentUser cannot be used here. The
// authenticated principal is on the connection instead (Context.User), populated
// by JWT bearer auth - the same source DefaultUserIdProvider reads for Clients.User.
internal static class HubUserExtensions
{
	public static long GetUserId(this HubCallerContext context)
	{
		var user = context.User
			?? throw new HubException("No authenticated user on the connection.");

		// JwtBearer maps the JWT "sub" claim onto ClaimTypes.NameIdentifier by
		// default; fall back to the raw "sub" string if left un-mapped.
		var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
			?? user.FindFirstValue("sub")
			?? throw new HubException("Authenticated user has no 'sub' claim.");

		if (!long.TryParse(sub, out var id))
			throw new HubException($"Authenticated user 'sub' claim ('{sub}') is not a valid snowflake ID.");

		return id;
	}
}
