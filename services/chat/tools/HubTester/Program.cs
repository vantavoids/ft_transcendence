using Cocona;
using Microsoft.AspNetCore.SignalR.Client;

const string DefaultChatHub = "https://localhost:1443/api/chat/v1/hubs/chat";
const string DefaultSignalingHub = "https://localhost:1443/api/chat/v1/hubs/signaling";

var app = CoconaApp.Create();

app.AddCommand("echo", async ([Argument] string message, string url = DefaultChatHub, string? token = null) =>
{
	await using var conn = Build(url, token);
	conn.On<string>("ReceiveMessage", m => Console.WriteLine($"<- ReceiveMessage: {m}"));

	await conn.StartAsync();
	Console.WriteLine($"-> Echo({message})");
	await conn.InvokeAsync("Echo", message);
	await Task.Delay(500);
}).WithDescription("invoke ChatHub.Echo and print the broadcast back. surely it will respond, right?");

app.AddCommand("listen", async (string url = DefaultChatHub, string? token = null, long[]? channel = null, int seconds = 60) =>
{
	await using var conn = Build(url, token);
	conn.On<object>("ReceiveMessage",       m => Console.WriteLine($"<- ReceiveMessage: {m}"));
	conn.On<object>("ReceiveDirectMessage", m => Console.WriteLine($"<- ReceiveDirectMessage: {m}"));
	conn.On<object>("MessageEdited",        m => Console.WriteLine($"<- MessageEdited: {m}"));
	conn.On<object>("MessageDeleted",       m => Console.WriteLine($"<- MessageDeleted: {m}"));
	conn.On<object>("UserPresence",         m => Console.WriteLine($"<- UserPresence: {m}"));
	conn.On<object, object>("GuildJoined",  (id, name) => Console.WriteLine($"<- GuildJoined: {id} ({name})"));
	conn.On<object>("GuildLeft",            id => Console.WriteLine($"<- GuildLeft: {id}"));
	conn.On<string, string>("Error",        (code, msg) => Console.WriteLine($"<- Error: {code} — {msg}"));

	await conn.StartAsync();
	Console.WriteLine($"connected to {url}");

	foreach (var channelId in channel ?? [])
	{
		await conn.InvokeAsync("JoinChannel", channelId);
		Console.WriteLine($"-> JoinChannel({channelId})");
	}

	Console.WriteLine($"listening {seconds}s...");
	await Task.Delay(TimeSpan.FromSeconds(seconds));
}).WithDescription("hold a connection open and print every server-pushed event");

app.AddCommand("signal-listen", async (string url = DefaultSignalingHub, string? token = null, int seconds = 60) =>
{
	await using var conn = Build(url, token);
	conn.On<object, object>("Offer",        (from, sdp)  => Console.WriteLine($"<- Offer from={from} sdp={sdp}"));
	conn.On<object, object>("Answer",       (from, sdp)  => Console.WriteLine($"<- Answer from={from} sdp={sdp}"));
	conn.On<object, object>("IceCandidate", (from, cand) => Console.WriteLine($"<- IceCandidate from={from} cand={cand}"));

	await conn.StartAsync();
	Console.WriteLine($"connected to {url}, listening {seconds}s...");
	await Task.Delay(TimeSpan.FromSeconds(seconds));
}).WithDescription("hold a connection to the signaling hub and print relayed frames.");

app.Run();

static HubConnection Build(string url, string? token) =>
	new HubConnectionBuilder()
		.WithUrl(url, opts =>
		{
			opts.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
			opts.SkipNegotiation = true;
			opts.HttpMessageHandlerFactory = _ => new HttpClientHandler
			{
				ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
			};
			opts.WebSocketConfiguration = ws =>
				ws.RemoteCertificateValidationCallback = (_, _, _, _) => true;
			if (token is not null)
				opts.AccessTokenProvider = () => Task.FromResult<string?>(token);
		})
		.Build();
