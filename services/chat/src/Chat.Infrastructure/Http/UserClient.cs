using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Chat.Application.Abstractions;

namespace Chat.Infrastructure.Http;

internal sealed class UserClient(HttpClient http) : IUserClient
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
	};

	public async Task<UsersRelationship?> GetUsersRelationship(long callerId, long recipientId, CancellationToken ct)
	{
		try
		{
			return await http.GetFromJsonAsync<UsersRelationship>(
				$"users/{callerId}/realtionship-with/{recipientId}", JsonOptions, ct);
		}
		// GetFromJsonAsync throws on non-2xx; map 404 to null so the handler's
		// `relationship is null -> UserNotFound` branch fires as intended
		catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
		{
			return null;
		}
	}
}
