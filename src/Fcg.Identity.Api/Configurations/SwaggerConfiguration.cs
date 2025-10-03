using System.Reflection;
using Asp.Versioning.ApiExplorer;
using Fcg.Identity.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Fcg.Identity.Api.Configurations;

public static class SwaggerConfig
{
    public static void AddSwaggerConfiguration(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddSwaggerGen(c =>
        {
            c.OperationFilter<SwaggerDefaultValues>();

            // Gera a lista de scopes dinamicamente a partir da nossa classe central
            var definedScopes = AppAuthorizationPolicies
                .Policies.SelectMany(p => p.Value) // Pega todos os scopes
                .Distinct() // Remove duplicados
                .ToDictionary(scope => scope, scope => $"Permissão para {scope}");

            c.AddSecurityDefinition(
                "oauth2",
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri(
                                $"{configuration["Jwt:Authority"]}/protocol/openid-connect/auth"
                            ),
                            TokenUrl = new Uri(
                                $"{configuration["Jwt:Authority"]}/protocol/openid-connect/token"
                            ),
                            Scopes = definedScopes,
                        },
                    },
                }
            );

            c.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    Description = "Insert the JWT token like this: Bearer {your token}",
                    Name = "Authorization",
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                }
            );

            c.OperationFilter<SecurityRequirementsOperationFilter>();

            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
        });
    }

    public static void UseSwaggerConfig(
        this IApplicationBuilder app,
        IApiVersionDescriptionProvider provider,
        IConfiguration configuration
    )
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint(
                    $"../swagger/{description.GroupName}/swagger.json",
                    description.GroupName.ToUpperInvariant()
                );
            }

            options.OAuthAppName("FCG Identity API - Swagger UI");
            options.OAuthUsePkce();
        });
    }
}

public class SecurityRequirementsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAllowAnonymous =
            context.MethodInfo.GetCustomAttribute<AllowAnonymousAttribute>() != null;
        if (hasAllowAnonymous)
            return;

        var policyNames = context
            .MethodInfo.GetCustomAttributes<AuthorizeAttribute>()
            .Select(attr => attr.Policy)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        var hasGenericAuthorize =
            context.MethodInfo.GetCustomAttribute<AuthorizeAttribute>() != null
            && policyNames.Count == 0;
        // Verifica se a controller tem [Authorize] e o método não tem política
        var controllerHasAuthorize =
            context.MethodInfo.DeclaringType?.GetCustomAttribute<AuthorizeAttribute>() != null
            && policyNames.Count == 0;

        // Se não houver políticas E nenhum Authorize genérico, o endpoint é público
        if (policyNames.Count == 0 && !hasGenericAuthorize && !controllerHasAuthorize)
        {
            return;
        }

        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });

        var requiredScopes = AppAuthorizationPolicies
            .Policies.Where(p => policyNames.Contains(p.Key))
            .SelectMany(p => p.Value)
            .Distinct()
            .ToList();

        operation.Security =
        [
            new()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "oauth2",
                        },
                    },
                    requiredScopes
                },
            },
            new()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer",
                        },
                    },
                    new List<string>()
                },
            },
        ];
    }
}

public class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
            options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
    }

    private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
    {
        var info = new OpenApiInfo
        {
            Title = "FCG Identity Service API",
            Version = description.ApiVersion.ToString(),
            Description = "Microservice responsible for user authentication and management.",
            Contact = new OpenApiContact
            {
                Name = "Vinicius Freire",
                Email = "vinicius_pinheiro05@hotmail.com",
            },
            License = new OpenApiLicense
            {
                Name = "MIT",
                Url = new Uri("https://opensource.org/licenses/MIT"),
            },
        };

        if (description.IsDeprecated)
            info.Description += " This API version is deprecated!";

        return info;
    }
}

public class SwaggerDefaultValues : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;
        operation.Deprecated |= apiDescription.IsDeprecated();

        if (operation.Parameters == null)
            return;

        foreach (var parameter in operation.Parameters)
        {
            var description = apiDescription.ParameterDescriptions.First(p =>
                p.Name == parameter.Name
            );
            parameter.Description ??= description.ModelMetadata.Description;
            if (parameter.Schema.Default == null && description.DefaultValue != null)
            {
                parameter.Schema.Default = OpenApiAnyFactory.CreateFromJson(
                    System.Text.Json.JsonSerializer.Serialize(
                        description.DefaultValue,
                        description.ModelMetadata.ModelType
                    )
                );
            }
            parameter.Required |= description.IsRequired;
        }
    }
}
