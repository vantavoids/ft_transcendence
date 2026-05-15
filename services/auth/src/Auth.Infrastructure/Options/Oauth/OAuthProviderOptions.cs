using System.ComponentModel.DataAnnotations;

namespace Auth.Infrastructure.Options.OAuth;

public abstract class OAuthProviderOptions
{
    [Required] public required string ClientId { get; init; }
    [Required] public required string ClientSecret { get; init; }
    [Required] public required string RedirectUri { get; init; }
    [Required] public virtual required string AuthorizationEndpoint { get; init; }
    [Required] public virtual required string TokenEndpoint { get; init; }
    [Required] public virtual required string UserEndpoint { get; init; }
}
