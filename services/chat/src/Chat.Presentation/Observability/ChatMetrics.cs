using System.Diagnostics.Metrics;
using Chat.Presentation.Hubs;

namespace Chat.Presentation.Observability;

/// <summary>
/// real-time operational gauges for Chat. unlike Guild's DB-backed gauges these
/// read live in-memory state (SignalR presence, the call registry), so the
/// observable callbacks are cheap and run directly on the scrape path - no
/// background collector needed.
/// </summary>
public sealed class ChatMetrics : IDisposable
{
	public const string MeterName = "Chat.Domain";

	private readonly Meter _meter;

	public ChatMetrics(IMeterFactory factory, UserConnectionTracker connections, CallRegistry calls)
	{
		_meter = factory.Create(MeterName);
		_meter.CreateObservableGauge("chat.hub.connected_users", () => connections.OnlineUserCount, description: "Users with an open chat-hub connection");
		_meter.CreateObservableGauge("chat.signaling.connected_users", () => calls.CountsSnapshot().SignalingUsers, description: "Users connected to the signaling hub");
		_meter.CreateObservableGauge("chat.calls.active", () => calls.CountsSnapshot().Active, description: "Calls currently tracked (ringing or in progress)");
		_meter.CreateObservableGauge("chat.calls.ringing", () => calls.CountsSnapshot().Ringing, description: "Calls ringing (unanswered)");
	}

	public void Dispose() => _meter.Dispose();
}
