using System.Net.Http.Json;
using System.Text.Json;

namespace Guild.FunctionalTests.Infrastructure;

/// <summary>small helpers to keep endpoint tests focused on what they assert</summary>
internal static class GuildClientExtensions
{
	public static async Task<JsonElement> CreateGuildAsync(
		this HttpClient client,
		string name,
		string? description = null,
		string? iconUrl = null,
		string? bannerUrl = null)
	{
		var resp = await client.PostAsJsonAsync(
			"/v1/guilds",
			new { name, description, icon_url = iconUrl, banner_url = bannerUrl },
			JsonOptions.SnakeCase);
		resp.EnsureSuccessStatusCode();
		var json = await resp.Content.ReadAsStringAsync();
		return JsonDocument.Parse(json).RootElement.Clone();
	}

	public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response)
	{
		var body = await response.Content.ReadAsStringAsync();
		return JsonDocument.Parse(body).RootElement.Clone();
	}
}
