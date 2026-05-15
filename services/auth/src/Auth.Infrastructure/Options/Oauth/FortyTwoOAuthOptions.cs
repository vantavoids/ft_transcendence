namespace Auth.Infrastructure.Options.OAuth;

public sealed class FortyTwoOAuthOptions : OAuthProviderOptions, IOptions
{
    public static string SectionName => "OAuth.FortyTwo";

    public override required string AuthorizationEndpoint
        { get; init; } = "https://api.intra.42.fr/oauth/authorize";

    public override required string TokenEndpoint
        { get; init; } = "https://api.intra.42.fr/oauth/token";

    public override required string UserEndpoint
        { get; init; } = "https://api.intra.42.fr/v2/me";
}
