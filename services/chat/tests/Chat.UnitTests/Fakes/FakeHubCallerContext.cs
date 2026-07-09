using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;

namespace Chat.UnitTests.Fakes;

/// <summary>records whether Abort() was called, so tests can assert a hard
/// disconnect was actually requested on the transport.</summary>
public sealed class FakeHubCallerContext : HubCallerContext
{
	public bool Aborted { get; private set; }

	public override string ConnectionId { get; } = Guid.NewGuid().ToString();
	public override string? UserIdentifier => null;
	public override ClaimsPrincipal? User => null;
	public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
	public override IFeatureCollection Features { get; } = new FeatureCollection();
	public override CancellationToken ConnectionAborted => CancellationToken.None;

	public override void Abort() => Aborted = true;
}
