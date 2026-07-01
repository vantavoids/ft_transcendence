using System.Text.Json;
using Guild.Application.Contracts;
using Xunit;

namespace Guild.UnitTests.Serialization;

/// <summary>
/// pins the JSON shape of the three RabbitMQ events Guild publishes. cross-service
/// consumers (Notification, Chat) deserialize these by snake_case property name,
/// so a rename or a serializer-policy drift breaks them silently at runtime. the
/// options here mirror the MassTransit config in
/// <c>Infrastructure/DependencyInjection.cs</c> (<c>ConfigureJsonSerializerOptions</c>
/// with <c>JsonNamingPolicy.SnakeCaseLower</c>).
/// </summary>
public sealed class EventSerializationTests
{
	private static readonly JsonSerializerOptions SnakeCase = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
	};

	[Fact]
	public void GuildMemberJoined_SerializesWith_SnakeCaseFields()
	{
		var json = JsonSerializer.Serialize(new GuildMemberJoined(100, "Test", 3), SnakeCase);

		Assert.Equal("{\"guild_id\":100,\"guild_name\":\"Test\",\"user_id\":3}", json);
	}

	[Fact]
	public void GuildMemberLeft_SerializesWith_SnakeCaseFields()
	{
		var json = JsonSerializer.Serialize(new GuildMemberLeft(100, 3), SnakeCase);

		Assert.Equal("{\"guild_id\":100,\"user_id\":3}", json);
	}

	[Fact]
	public void GuildInviteCreated_SerializesWith_SnakeCaseFields()
	{
		var json = JsonSerializer.Serialize(new GuildInviteCreated(100, "Test", 1, 7), SnakeCase);

		Assert.Equal(
			"{\"guild_id\":100,\"guild_name\":\"Test\",\"invited_by_user_id\":1,\"invited_user_id\":7}",
			json);
	}

	[Fact]
	public void GuildInviteCreated_WithNullTarget_EmitsNullInvitedUserId()
	{
		var json = JsonSerializer.Serialize(new GuildInviteCreated(100, "Test", 1, null), SnakeCase);

		Assert.Equal(
			"{\"guild_id\":100,\"guild_name\":\"Test\",\"invited_by_user_id\":1,\"invited_user_id\":null}",
			json);
	}

	[Fact]
	public void GuildMemberJoined_RoundTrips()
	{
		var original = new GuildMemberJoined(100, "Test", 3);
		var json = JsonSerializer.Serialize(original, SnakeCase);

		Assert.Equal(original, JsonSerializer.Deserialize<GuildMemberJoined>(json, SnakeCase));
	}

	[Fact]
	public void GuildMemberLeft_RoundTrips()
	{
		var original = new GuildMemberLeft(100, 3);
		var json = JsonSerializer.Serialize(original, SnakeCase);

		Assert.Equal(original, JsonSerializer.Deserialize<GuildMemberLeft>(json, SnakeCase));
	}

	[Fact]
	public void GuildInviteCreated_RoundTrips()
	{
		var original = new GuildInviteCreated(100, "Test", 1, null);
		var json = JsonSerializer.Serialize(original, SnakeCase);

		Assert.Equal(original, JsonSerializer.Deserialize<GuildInviteCreated>(json, SnakeCase));
	}

	[Fact]
	public void GuildOwnerTransferred_SerializesWith_SnakeCaseFields()
	{
		var json = JsonSerializer.Serialize(new GuildOwnerTransferred(100, 1, 2), SnakeCase);

		Assert.Equal("{\"guild_id\":100,\"old_owner_id\":1,\"new_owner_id\":2}", json);
	}

	[Fact]
	public void GuildOwnerTransferred_RoundTrips()
	{
		var original = new GuildOwnerTransferred(100, 1, 2);
		var json = JsonSerializer.Serialize(original, SnakeCase);

		Assert.Equal(original, JsonSerializer.Deserialize<GuildOwnerTransferred>(json, SnakeCase));
	}
}
