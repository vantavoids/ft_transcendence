using Auth.Domain.AuthUser;

namespace Auth.Infrastructure.OAuth;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class OAuthProviderKeyAttribute(OAuthProvider provider) : Attribute
{
    public OAuthProvider Provider => provider;
}
