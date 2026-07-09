using Chat.Infrastructure;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Chat.FunctionalTests.Infrastructure;

internal static class HubConnectionHelper
{
	public static HubConnection Build(ChatApiFactory factory, string? token, string path = "/v1/hubs/chat")
	{
		var server = factory.Server;
		var hubUri = new UriBuilder(server.BaseAddress) { Path = path }.Uri;

		return new HubConnectionBuilder()
			.WithUrl(hubUri, options =>
			{
				options.Transports = HttpTransportType.WebSockets;
				options.SkipNegotiation = true;
				options.HttpMessageHandlerFactory = _ => server.CreateHandler();
				options.WebSocketFactory = async (context, ct) =>
				{
					var wsClient = server.CreateWebSocketClient();
					wsClient.ConfigureRequest = req =>
					{
						if (token is not null)
							req.Headers["Authorization"] = $"Bearer {token}";
					};

					var uri = context.Uri;
					if (token is not null)
					{
						var separator = string.IsNullOrEmpty(uri.Query) ? "?" : "&";
						uri = new Uri(uri + separator + "access_token=" + token);
					}

					return await wsClient.ConnectAsync(uri, ct);
				};
			})
			// server payloads are snake_case with quoted snowflakes (see Program.cs
			// AddJsonProtocol); match that here rather than relying on case-insensitive
			// fallback, since the deserializer used against typed test DTOs otherwise
			// silently leaves properties null (or throws on quoted longs) instead of erroring clearly.
			.AddJsonProtocol(o =>
			{
				o.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
				o.PayloadSerializerOptions.Converters.Add(new SnowflakeJsonConverter());
			})
			.Build();
	}
}
