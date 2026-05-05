using Auth.Infrastructure.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Auth.Presentation;

public static class DependencyInjection
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer();

        services.AddAuthorization();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtOptions>>((bearerOpts, jwtOpts) =>
                {
                    var jwt = jwtOpts.Value;
                    var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey));
                    bearerOpts.MapInboundClaims = false;
                    bearerOpts.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer           = true,
                        ValidIssuer              = jwt.Issuer,
                        ValidateAudience         = true,
                        ValidAudience            = jwt.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey         = secretKey,
                        ValidateLifetime         = true,
                        ClockSkew                = TimeSpan.Zero,
                    };
                });

        return services;
    }
}
