using System.Linq;
using System.Text.Json;
using Guild.Application;
using Guild.Infrastructure.Messaging.Contracts;
using Xunit;

namespace Guild.UnitTests.Serialization;

/// <summary>
/// pins the wire identity of the inbound <c>user.deleted</c> event. Auth publishes
/// its <c>UserDeleted</c> type (namespace <c>Auth.Application.Contracts</c>) under
/// the URN and snake_case payload asserted here; if Guild's inbound binding drifts
/// from that, MassTransit silently routes the message to the *_skipped queue and
/// the GDPR cascade never runs. these tests fail loudly instead.
/// </summary>
public sealed class UserDeletedContractTests
{
	private static readonly JsonSerializerOptions Wire =
		GuildSerialization.ApplyEventWireFormat(new());

	[Fact]
	public void UserDeleted_BindsTo_AuthPublisherUrn()
	{
		// read the [MessageUrn] via reflection to avoid a compile-time MassTransit
		// reference in the test assembly. the full urn is "urn:message:" + value.
		var attribute = typeof(UserDeleted)
			.GetCustomAttributes(inherit: false)
			.Single(a => a.GetType().Name == "MessageUrnAttribute");

		var urn = attribute.GetType().GetProperty("Urn")!.GetValue(attribute)!;

		Assert.Equal("urn:message:Auth.Application.Contracts:UserDeleted", urn.ToString());
	}

	[Fact]
	public void UserDeleted_DeserializesFrom_StringIdPayload()
	{
		var evt = JsonSerializer.Deserialize<UserDeleted>("{\"user_id\":\"123\"}", Wire);

		Assert.NotNull(evt);
		Assert.Equal(123, evt!.UserId);
	}

	[Fact]
	public void UserDeleted_DeserializesFrom_LegacyNumberIdPayload()
	{
		var evt = JsonSerializer.Deserialize<UserDeleted>("{\"user_id\":123}", Wire);

		Assert.NotNull(evt);
		Assert.Equal(123, evt!.UserId);
	}

	[Fact]
	public void UserDeleted_SerializesWith_SnakeCaseField()
	{
		var json = JsonSerializer.Serialize(new UserDeleted(123), Wire);

		Assert.Equal("{\"user_id\":\"123\"}", json);
	}
}
