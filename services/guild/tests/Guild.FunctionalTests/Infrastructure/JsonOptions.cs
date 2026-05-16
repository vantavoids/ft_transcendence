using System.Text.Json;

namespace Guild.FunctionalTests.Infrastructure;

/// <summary>
/// snake_case-configured options used both to serialise request bodies and to
/// deserialise responses. Matches the policy applied server-side via
/// <c>ConfigureHttpJsonOptions</c> in <c>Program.cs</c>
/// </summary>
internal static class JsonOptions
{
	public static readonly JsonSerializerOptions SnakeCase = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
	};
}
