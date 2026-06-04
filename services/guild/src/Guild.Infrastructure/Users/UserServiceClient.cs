using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Guild.Application.Abstractions.Users;
using Microsoft.Extensions.Logging;

namespace Guild.Infrastructure.Users;

/// <summary>
/// hits the User Service internal endpoints over the docker network. swallows
/// connection failures and 404s with a warn log so the caller never sees a
/// transport-level exception. see <see cref="IUserService"/> for the contract
/// </summary>
internal sealed class UserServiceClient(HttpClient http, ILogger<UserServiceClient> logger) : IUserService
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
	};

	public async Task<bool> ExistsAsync(long userId, CancellationToken cancellationToken = default)
	{
		try
		{
			using var response = await http.GetAsync($"/internal/users/{userId}", cancellationToken);
			if (response.StatusCode == HttpStatusCode.NotFound)
				return false;
			response.EnsureSuccessStatusCode();
			return true;
		}
		catch (HttpRequestException ex)
		{
			logger.LogWarning(ex, "user service unreachable for ExistsAsync({UserId}); treating as best-effort", userId);
			return true;
		}
		catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
		{
			logger.LogWarning(ex, "user service timed out for ExistsAsync({UserId}); treating as best-effort", userId);
			return true;
		}
	}

	public async Task<UserSummary?> GetSummaryAsync(long userId, CancellationToken cancellationToken = default)
	{
		try
		{
			using var response = await http.GetAsync($"/internal/users/{userId}", cancellationToken);
			if (response.StatusCode == HttpStatusCode.NotFound)
				return null;
			response.EnsureSuccessStatusCode();

			var payload = await response.Content.ReadFromJsonAsync<InternalUserPayload>(
				JsonOptions, cancellationToken);
			if (payload is null || string.IsNullOrEmpty(payload.Id))
				return null;
			if (!long.TryParse(payload.Id, out var id))
				return null;
			return new UserSummary(id, payload.Username ?? string.Empty);
		}
		catch (HttpRequestException ex)
		{
			logger.LogWarning(ex, "user service unreachable for GetSummaryAsync({UserId})", userId);
			return null;
		}
		catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
		{
			logger.LogWarning(ex, "user service timed out for GetSummaryAsync({UserId})", userId);
			return null;
		}
	}

	private sealed record InternalUserPayload(string Id, string? Username);
}
