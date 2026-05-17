using System.Security.Claims;
using Auth.Application.Abstractions.Security;
using Microsoft.AspNetCore.Http;

namespace Auth.Infrastructure.Security;

internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
	public long Id
	{
		get
		{
			var user = accessor.HttpContext?.User
				?? throw new InvalidOperationException("No HttpContext is available.");

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
