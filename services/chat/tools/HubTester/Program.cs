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
	conn.On<object>("GuildLeft",            id => Console.WriteLine($"<- GuildLeft: {id}"));
	conn.On<object,
			object>("GuildJoined",   (id, name) => Console.WriteLine($"<- GuildJoined: {id} ({name})"));
	conn.On<string,
			string,
			string,
			object>("TypingStarted", (userId, scope, id, until) => Console.WriteLine($"<- TypingStarted: user={userId} scope={scope} id={id} until={until}"));
	conn.On<string, string>("Error", (code, msg) => Console.WriteLine($"<- Error: {code} — {msg}"));

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

app.AddCommand("send-typing", async ([Argument] string scope, [Argument] long id, string url = DefaultChatHub, string? token = null) =>
{
	await using var conn = Build(url, token);
	conn.On<string, string>("Error", (code, msg) => Console.WriteLine($"<- Error: {code} — {msg}"));

	await conn.StartAsync();
	Console.WriteLine($"connected to {url}");

	if (scope == "channel")
	{
		await conn.InvokeAsync("JoinChannel", id);
		Console.WriteLine($"-> JoinChannel({id})");
	}

	Console.WriteLine("press Enter to send Typing, Esc to quit");
	while (true)
	{
		var key = Console.ReadKey(intercept: true);
		if (key.Key == ConsoleKey.Escape) break;
		if (key.Key != ConsoleKey.Enter) continue;

		await conn.InvokeAsync("Typing", scope, id);
		Console.WriteLine($"-> Typing({scope}, {id})");
	}
}).WithDescription("join a channel (if scope=channel) and invoke Typing interactively — Enter sends, Esc quits");

app.AddCommand("session", async (string url = DefaultChatHub, string? token = null) =>
{
	await using var conn = Build(url, token);
	conn.On<object>("ReceiveMessage",       m => Console.WriteLine($"<- ReceiveMessage: {m}"));
	conn.On<object>("ReceiveDirectMessage", m => Console.WriteLine($"<- ReceiveDirectMessage: {m}"));
	conn.On<object>("MessageEdited",        m => Console.WriteLine($"<- MessageEdited: {m}"));
	conn.On<object>("MessageDeleted",       m => Console.WriteLine($"<- MessageDeleted: {m}"));
	conn.On<object>("UserPresence",         m => Console.WriteLine($"<- UserPresence: {m}"));
	conn.On<object>("GuildLeft",            id => Console.WriteLine($"<- GuildLeft: {id}"));
	conn.On<object, object>("GuildJoined",  (id, name) => Console.WriteLine($"<- GuildJoined: {id} ({name})"));
	conn.On<string, string, string, object>("TypingStarted",
		(userId, scope, id, until) => Console.WriteLine($"<- TypingStarted: user={userId} scope={scope} id={id} until={until}"));
	conn.On<string, string>("Error", (code, msg) => Console.WriteLine($"<- Error: {code} — {msg}"));

	await conn.StartAsync();
	Console.WriteLine($"connected to {url}");
	Console.WriteLine("commands: join <id> | leave <id> | typing <channel|dm> <id> | quit");

	while (true)
	{
		Console.Write("> ");
		var line = Console.ReadLine()?.Trim();
		if (line is null or "" or "quit" or "exit") break;

		var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		try
		{
			switch (parts[0])
			{
				case "join" when parts.Length == 2 && long.TryParse(parts[1], out var joinId):
					await conn.InvokeAsync("JoinChannel", joinId);
					Console.WriteLine($"-> JoinChannel({joinId})");
					break;

				case "leave" when parts.Length == 2 && long.TryParse(parts[1], out var leaveId):
					await conn.InvokeAsync("LeaveChannel", leaveId);
					Console.WriteLine($"-> LeaveChannel({leaveId})");
					break;

				case "typing" when parts.Length == 3 && long.TryParse(parts[2], out var typingId):
					await conn.InvokeAsync("Typing", parts[1], typingId);
					Console.WriteLine($"-> Typing({parts[1]}, {typingId})");
					break;

				default:
					Console.WriteLine("usage: join <id> | leave <id> | typing <channel|dm> <id> | quit");
					break;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"error: {ex.Message}");
		}
	}
}).WithDescription("interactive session: stay connected and send hub invocations (join, leave, typing, echo)");

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
