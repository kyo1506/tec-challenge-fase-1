using System.Reflection;
using Asp.Versioning.ApiExplorer;
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

            // Adds the security definition for the OAuth2/OpenID Connect flow
            c.AddSecurityDefinition(
                "oauth2",
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            // URL for the authorization endpoint of your Keycloak realm
                            AuthorizationUrl = new Uri(
                                $"{configuration["Jwt:Authority"]}/protocol/openid-connect/auth"
                            ),
                            // URL for the token endpoint of your Keycloak realm
                            TokenUrl = new Uri(
                                $"{configuration["Jwt:Authority"]}/protocol/openid-connect/token"
                            ),
                            Scopes = new Dictionary<string, string>
                            {
                                // Maps the scopes (permissions) to user-friendly descriptions in the Swagger UI
                                { "users:read", "Permission to read user data" },
                                {
                                    "users:manage",
                                    "Permission to manage users (create/edit/delete)"
                                },
                                // Add other scopes from your application here
                            },
                        },
                    },
                }
            );

            // Adds a filter to apply the security requirement (the lock icon)
            // automatically to endpoints that have the [Authorize] attribute
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

            // Configures the Swagger UI to use the OAuth2 authorization flow
            options.OAuthClientId(configuration["Swagger:ClientId"]);
            options.OAuthAppName("FCG Identity API - Swagger UI");
            options.OAuthUsePkce(); // Enables the recommended PKCE security standard
        });
    }
}

public class SecurityRequirementsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Checks if the endpoint method has the [Authorize] attribute
        var hasAuthorize =
            context.MethodInfo.DeclaringType.GetCustomAttribute<AuthorizeAttribute>() != null
            || context.MethodInfo.GetCustomAttribute<AuthorizeAttribute>() != null;

        if (hasAuthorize)
        {
            // Adds the 401 (Unauthorized) response to the endpoint's documentation
            operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
            // Adds the 403 (Forbidden) response to the endpoint's documentation
            operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });

            // Defines the OAuth2 security requirement for this endpoint
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
                        context
                            .MethodInfo.GetCustomAttributes<AuthorizeAttribute>()
                            .Select(attr => attr.Policy)
                            .Where(p => p != null)
                            .ToList()
                    },
                },
            ];
        }
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
