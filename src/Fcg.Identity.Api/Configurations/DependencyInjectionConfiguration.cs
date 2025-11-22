using Fcg.Identity.Api.Extensions;
using Fcg.Identity.Domain.Notifications;
using Fcg.Identity.Infrastructure.Extensions;
using Fcg.Identity.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace Fcg.Identity.Api.Configurations;

public static class DependencyInjectionConfiguration
{
    public static void ResolveDependencies(this IServiceCollection services)
    {
        services.AddScoped<INotifier, Notifier>();

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        services.AddScoped<IUser, AspNetUser>();

        // Configura o serviço e o HttpClient para interagir com o Keycloak
        services.Configure<KeycloakConfiguration>(
            services
                .BuildServiceProvider()
                .GetRequiredService<IConfiguration>()
                .GetSection(nameof(KeycloakConfiguration))
        );

        services.AddHttpClient<IKeycloakService, KeycloakService>(
            (serviceProvider, client) =>
            {
                var config = serviceProvider
                    .GetRequiredService<IOptions<KeycloakConfiguration>>()
                    .Value;
                client.BaseAddress = new Uri(config.BaseUrl);
            }
        );

        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddProblemDetails();
    }
}
