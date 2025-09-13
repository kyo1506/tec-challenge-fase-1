using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using TecChallenge.Application.Extensions;
using TecChallenge.Data.Contexts;
using TecChallenge.Data.Repositories;
using TecChallenge.Data.UnitOfWork;
using TecChallenge.Domain.Notifications;
using TecChallenge.Domain.Services;
using TecChallenge.Infrastructure.Services;
using TecChallenge.Infrastructure.Services.Keycloak;

namespace TecChallenge.Application.Configurations;

public static class DependencyInjectionConfiguration
{
    public static void ResolveDependencies(this IServiceCollection services)
    {
        services.AddScoped<AppDbContext>();

        services.AddScoped<INotifier, Notifier>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IUserLibraryRepository, UserLibraryRepository>();
        services.AddScoped<IPromotionRepository, PromotionRepository>();
        services.AddScoped<IUserWalletRepository, UserWalletRepository>();
        services.AddScoped<IWalletTransactionRepository, WalletTransactionRepository>();
        services.AddScoped<IPromotionGameRepository, PromotionGameRepository>();

        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IUserLibraryService, UserLibraryService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IPromotionService, PromotionService>();

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        services.AddSingleton<IMockEmailService, MockEmailService>();

        services.AddScoped<IUser, AspNetUser>();

        // Configura o serviço e o HttpClient para interagir com o Keycloak
        services.Configure<KeycloakAdminConfig>(
            services
                .BuildServiceProvider()
                .GetRequiredService<IConfiguration>()
                .GetSection("KeycloakAdmin")
        );

        services.AddHttpClient<IKeycloakAdminService, KeycloakAdminService>(
            (serviceProvider, client) =>
            {
                var config = serviceProvider
                    .GetRequiredService<IOptions<KeycloakAdminConfig>>()
                    .Value;
                client.BaseAddress = new Uri(config.BaseUrl);
            }
        );

        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddProblemDetails();

        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
    }
}
