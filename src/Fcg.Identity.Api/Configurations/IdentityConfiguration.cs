using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Fcg.Identity.Api.Configurations;

public static class IdentityConfig
{
    public static void AddIdentityConfiguration(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // URL do seu realm no Keycloak
                options.Authority = configuration["Jwt:Authority"];

                // Client ID que você criou no Keycloak para esta API
                options.Audience = configuration["Jwt:Audience"];

                // Em desenvolvimento, podemos desabilitar a verificação de HTTPS
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Valida se a assinatura do token é confiável
                    ValidateIssuerSigningKey = true,
                    // Valida quem emitiu o token (o seu Keycloak)
                    ValidateIssuer = true,
                    // Valida para quem o token foi emitido (sua API)
                    ValidateAudience = true,
                    // Valida o tempo de vida do token
                    ValidateLifetime = true,
                    // Remove a tolerância de tempo (clock skew) para validação da expiração
                    ClockSkew = TimeSpan.Zero,
                };
            });

        // Configura as políticas de autorização baseadas nos papéis (roles) do Keycloak
        services
            .AddAuthorizationBuilder()
            .AddPolicy("CanManageUsers", policy => policy.RequireClaim("scope", "users:manage"))
            .AddPolicy(
                "CanReadUsers",
                policy =>
                {
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(c =>
                            c.Type == "scope" && c.Value.Contains("users:read")
                        )
                        || context.User.HasClaim(c =>
                            c.Type == "scope" && c.Value.Contains("users:manage")
                        )
                    );
                }
            );
    }
}
