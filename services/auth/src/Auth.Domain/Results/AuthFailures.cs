namespace Auth.Domain.Results;

public static class AuthFailures
{
    public static readonly Failure InvalidEmail =
        new("Auth.InvalidEmail", "Invalid email format.");

    public static readonly Failure WeakPassword =
        new("Auth.WeakPassword", "Password does not meet security requirements.");

    public static readonly Failure EmailAlreadyRegistered =
        new("Auth.EmailAlreadyRegistered", "Email is already registered.");

    public static readonly Failure InvalidCredentials =
        new("Auth.InvalidCredentials", "Invalid credentials.");

    public static readonly Failure InvalidAccessToken =
        new("Auth.InvalidAccessToken", "Access token is invalid.");
        
    public static readonly Failure InvalidRefreshToken =
        new("Auth.InvalidRefreshToken", "Refresh token is invalid.");

    public static readonly Failure InvalidOAuthProvider =
        new("Auth.InvalidOAuthProvider", "OAuth provider is invalid.");

    public static readonly Failure InvalidOAuthId =
        new("Auth.InvalidOAuthId", "OAuth identifier is invalid.");

    public static readonly Failure InvalidAuthUserState =
        new("Auth.InvalidAuthUserState", "Auth user state is invalid.");

    public static readonly Failure InvalidOAuthState =
        new("Auth.InvalidOAuthState", "OAuth state is invalid or expired.");

    public static readonly Failure OAuthProviderError =
        new("Auth.OAuthProviderError", "OAuth provider returned an error.");

    public static readonly Failure OAuthUpstreamError =
        new("Auth.OAuthUpstreamError", "OAuth provider is temporarily unavailable.");

    public static readonly Failure OAuthCantPatchEmail =
        new("Auth.OAuthCantPatchEmail", "Cannot patch email for OAuth user.");

    public static readonly Failure AtLeastOneFieldToPatch =
        new("Auth.AtLeastOneFieldToPatch", "No field provided, at least one required");
}
