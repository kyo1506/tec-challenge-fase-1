using Fcg.Identity.Api.Extensions;
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

                // Configuração do MetadataAddress para buscar as chaves públicas
                options.MetadataAddress =
                    $"{configuration["Jwt:Authority"]}/.well-known/openid-configuration";

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Valida se a assinatura do token é confiável
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    // Aceita múltiplos issuers (localhost e keycloak container)
                    ValidIssuers =
                    [
                        configuration["Jwt:Authority"], // http://keycloak:8080/realms/fiap-cloud-games
                        configuration["Jwt:Authority"]?.Replace("keycloak:8080", "localhost:8080"), // http://localhost:8080/realms/fiap-cloud-games
                    ],
                    ValidateAudience = true,
                    ValidAudiences = [configuration["Jwt:Audience"]],
                    ValidateLifetime = true,
                    // Remove a tolerância de tempo (clock skew) para validação da expiração
                    ClockSkew = TimeSpan.Zero,
                };
            });

        // Configura as políticas de autorização baseadas nos scopes do token JWT
        // Apenas 3 permissões essenciais: users:manage, users:read e profiles:manage
        services
            .AddAuthorizationBuilder()
            .AddPolicy(
                "CanManageUsers",
                policy =>
                    policy.RequireAssertion(context =>
                        context.User.Identity != null
                        && context.User.Identity.IsAuthenticated
                        && context.User.HasClaim(c =>
                            c.Type == "scope" && c.Value.Split(' ').Contains("users:manage")
                        )
                    )
            )
            .AddPolicy(
                "CanReadUsers",
                policy =>
                    policy.RequireAssertion(context =>
                        context.User.Identity?.IsAuthenticated == true
                        && context.User.HasClaim(c =>
                            c.Type == "scope"
                            && (
                                c.Value.Split(' ').Contains("users:read")
                                || c.Value.Split(' ').Contains("users:manage")
                            )
                        )
                    )
            )
            .AddPolicy(
                "CanManageProfile",
                policy =>
                    policy.RequireAssertion(context =>
                        context.User.Identity != null
                        && context.User.Identity.IsAuthenticated
                        && context.User.HasClaim(c =>
                            c.Type == "scope" && c.Value.Split(' ').Contains("profiles:manage")
                        )
                    )
            );
    }
}
