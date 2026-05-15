namespace Auth.Domain.AuthUser;

#nullable enable
public sealed record OAuthUserInfo(string ProviderId, string? Email, bool? EmailVerified);