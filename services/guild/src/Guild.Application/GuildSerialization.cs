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

	/// <summary>
	/// wire format for RabbitMQ event payloads: snake_case names plus snowflake
	/// ids as quoted strings (docs/contracts ID policy). applied by the MassTransit
	/// serializer config and mirrored by the serialization unit tests. HTTP does
	/// not use this: response DTOs type their ids as strings explicitly and keep
	/// the <c>permissions</c> bitmask numeric.
	/// </summary>
	public static JsonSerializerOptions ApplyEventWireFormat(JsonSerializerOptions options)
	{
		options.PropertyNamingPolicy = NamingPolicy;
		options.Converters.Add(new SnowflakeJsonConverter());
		return options;
	}
}
