using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Auth.Application.Abstractions.OAuth;
using Auth.Domain.AuthUser;
using Auth.Domain.Results;

namespace Auth.Infrastructure.OAuth;

internal abstract class OAuthProviderBase(HttpClient http) : IOAuthProviderClient
{
    private static readonly ProductInfoHeaderValue UserAgent = new("ft_transcendence", "1.0");

    public abstract Uri BuildAuthorizationUrl(string state);
    public abstract Task<Result<OAuthUserInfo>> ExchangeCodeAsync(
        string code, string state, CancellationToken cancellationToken = default);

    protected async Task<Result<T>> FetchToken<T>(
        string tokenEndpoint, Dictionary<string, string> formContent, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(formContent)
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return await Send<T>(request, ct);
    }

    protected async Task<Result<T>> FetchAuthenticated<T>(
        string url, string accessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.Add(UserAgent);
        return await Send<T>(request, ct);
    }

    protected async Task<Result<T>> Send<T>(HttpRequestMessage request, CancellationToken ct)
    {
        // ? Intentionnaly let the exception throw => 500
        using var response = await http.SendAsync(request, ct); 

        if (!response.IsSuccessStatusCode)
            return (int)response.StatusCode >= 500
                ? AuthFailures.OAuthUpstreamError
                : AuthFailures.OAuthProviderError;

        try
        {
            var data = await response.Content.ReadFromJsonAsync<T>(ct);
            return data is null
                ? AuthFailures.OAuthUpstreamError
                : data;
        }
        catch (JsonException)
        {
            return AuthFailures.OAuthUpstreamError;
        }
    }

    protected static string BuildQueryString(params (string key, string value)[] pairs)
        => string.Join("&", pairs.Select(p =>
            $"{Uri.EscapeDataString(p.key)}={Uri.EscapeDataString(p.value)}"));
}
