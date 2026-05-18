using System.Security.Claims;
using Chat.Application.Abstractions.Authentication;
using Microsoft.AspNetCore.Http;

namespace Chat.Infrastructure.Authentication;

internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
	public long UserId
	{
		get
		{
			var user = accessor.HttpContext?.User
				?? throw new InvalidOperationException("No HttpContext is available.");

			// JwtBearer maps the JWT "sub" claim onto ClaimTypes.NameIdentifier
			// by default; fall back to the raw "sub" string if a custom token
			// validator left the claim un-mapped
			var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
				?? user.FindFirstValue("sub")
				?? throw new InvalidOperationException("Authenticated user has no 'sub' claim.");

			if (!long.TryParse(sub, out var id))
				throw new InvalidOperationException(
					$"Authenticated user 'sub' claim ('{sub}') is not a valid snowflake ID.");

			return id;
		}
	}
}
