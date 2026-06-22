namespace Auth.Domain.AuthUser;

public static class OAuthProviderExtensions
{
    public static string ToSlug(this OAuthProvider provider) => provider switch
    {
        OAuthProvider.Github   => "github",
        OAuthProvider.Google   => "google",
        OAuthProvider.FortyTwo => "fortytwo",
        _                      => provider.ToString().ToLowerInvariant()
    };

    public static bool TryFromSlug(string slug, out OAuthProvider provider)
    {
        switch (slug.ToLowerInvariant())
        {
            case "github":   provider = OAuthProvider.Github;   return true;
            case "google":   provider = OAuthProvider.Google;   return true;
            case "fortytwo": provider = OAuthProvider.FortyTwo; return true;
            default:         provider = OAuthProvider.Unknown;  return false;
        }
    }
}
