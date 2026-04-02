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

    public static readonly Failure MissingRefreshToken =
        new("Auth.MissingRefreshToken", "Refresh token is missing.");

    public static readonly Failure InvalidRefreshToken =
        new("Auth.InvalidRefreshToken", "Refresh token is invalid.");

    public static readonly Failure ExpiredRefreshToken =
        new("Auth.ExpiredRefreshToken", "Refresh token is expired.");

    public static readonly Failure RevokedRefreshToken =
        new("Auth.RevokedRefreshToken", "Refresh token is revoked.");

    public static readonly Failure InvalidOAuthProvider =
        new("Auth.InvalidOAuthProvider", "OAuth provider is invalid.");

    public static readonly Failure InvalidOAuthId =
        new("Auth.InvalidOAuthId", "OAuth identifier is invalid.");

    public static readonly Failure InvalidAuthUserState =
        new("Auth.InvalidAuthUserState", "Auth user state is invalid.");
}
