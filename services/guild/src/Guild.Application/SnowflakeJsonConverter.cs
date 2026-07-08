using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Guild.Application;

/// <summary>
/// ID policy (docs/contracts): snowflake ids travel as quoted strings on the
/// JSON wire. writes longs as strings; reads both forms so inbound events from
/// publishers that still send bare numbers keep deserializing.
/// </summary>
internal sealed class SnowflakeJsonConverter : JsonConverter<long>
{
	public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		reader.TokenType == JsonTokenType.String
			? long.Parse(reader.GetString()!, CultureInfo.InvariantCulture)
			: reader.GetInt64();

	public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options) =>
		writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
}
