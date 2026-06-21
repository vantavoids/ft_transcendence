namespace Auth.Domain.AuthUser;

public sealed record OAuthUserInfo(string ProviderId, string? Email, bool? EmailVerified);