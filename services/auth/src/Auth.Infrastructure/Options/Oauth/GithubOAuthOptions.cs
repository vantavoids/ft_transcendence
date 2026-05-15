namespace Auth.Infrastructure.Options.OAuth;

public sealed class GithubOAuthOptions : OAuthProviderOptions, IOptions
{
    public static string SectionName => "OAuth.Github";

    public override required string AuthorizationEndpoint
        { get; init; } = "https://github.com/login/oauth/authorize";

    public override required string TokenEndpoint
        { get; init; } = "https://github.com/login/oauth/access_token";

    public override required string UserEndpoint
        { get; init; } = "https://api.github.com/user";

    public required string EmailEndpoint
        { get; init; } = "https://api.github.com/user/emails";
}
