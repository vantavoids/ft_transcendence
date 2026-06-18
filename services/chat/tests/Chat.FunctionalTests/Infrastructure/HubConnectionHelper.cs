using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Chat.FunctionalTests.Infrastructure;

internal static class HubConnectionHelper
{
	public static HubConnection Build(ChatApiFactory factory, string? token)
	{
		var server = factory.Server;
		var hubUri = new UriBuilder(server.BaseAddress) { Path = "/v1/hubs/chat" }.Uri;

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
			.Build();
	}
}
