namespace Auth.Infrastructure.Options.OAuth;

public sealed class GoogleOAuthOptions : OAuthProviderOptions, IOptions
{
    public static string SectionName => "OAuth:Google";

    public override required string AuthorizationEndpoint
        { get; init; } = "https://accounts.google.com/o/oauth2/auth";

    public override required string TokenEndpoint
        { get; init; } = "https://oauth2.googleapis.com/token";

    public override required string UserEndpoint
        { get; init; } = "null";
}
