using System.Text.Json;
using Guild.Presentation.Endpoints;
using Xunit;

namespace Guild.UnitTests.Serialization;

/// <summary>
/// locks the on-the-wire shape of <see cref="ErrorBody"/>. every client parses
/// the <c>"error"</c> field; a serializer-policy change or a property rename
/// would silently break all of them, so the shape is pinned here rather than
/// discovered in production. mirrors the snake_case policy applied in
/// <c>Program.cs</c> via <c>ConfigureHttpJsonOptions</c>
/// </summary>
public sealed class ErrorBodySerializationTests
{
	private static readonly JsonSerializerOptions SnakeCase = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
	};

	[Fact]
	public void ErrorBody_SerializesTo_SingleSnakeCaseErrorField()
	{
		var json = JsonSerializer.Serialize(new ErrorBody("Guild not found."), SnakeCase);

		Assert.Equal("{\"error\":\"Guild not found.\"}", json);
	}

	[Fact]
	public void ErrorBody_RoundTrips()
	{
		var original = new ErrorBody("Caller is missing the required permission.");

		var json = JsonSerializer.Serialize(original, SnakeCase);
		var restored = JsonSerializer.Deserialize<ErrorBody>(json, SnakeCase);

		Assert.Equal(original, restored);
	}
}
