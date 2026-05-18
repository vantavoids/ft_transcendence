using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Chat.FunctionalTests.Infrastructure;

/// <summary>
/// issues HS256-signed JWTs whose <c>sub</c> claim drives the
/// <c>CurrentUser</c> binding in the running pipeline. the signing key must
/// match <c>ChatApiFactory.JwtSecret</c> so the bearer middleware accepts it
/// </summary>
internal static class TestTokens
{
	public static string Issue(string secret, long userId, DateTimeOffset? expiresAt = null)
	{
		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
		var now = DateTimeOffset.UtcNow;
		var exp = expiresAt ?? now.AddHours(1);

		var claims = new[]
		{
			new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
			new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(),
				ClaimValueTypes.Integer64),
		};

		var token = new JwtSecurityToken(
			claims: claims,
			expires: exp.UtcDateTime,
			signingCredentials: creds);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}
