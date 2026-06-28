using System.Text.Json;

namespace Guild.Application;

/// <summary>
/// the single source of truth for the Guild service's JSON wire format. both the
/// HTTP pipeline (<c>Program.cs</c> via <c>ConfigureHttpJsonOptions</c>) and the
/// MassTransit message serializer (<c>Infrastructure/DependencyInjection.cs</c>)
/// apply <see cref="NamingPolicy"/>, so API responses and event payloads can
/// never drift apart - cross-service consumers rely on the snake_case property
/// names matching <c>docs/contracts</c>.
/// </summary>
public static class GuildSerialization
{
	public static readonly JsonNamingPolicy NamingPolicy = JsonNamingPolicy.SnakeCaseLower;
}
