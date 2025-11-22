using Serilog;
using Serilog.Events;

namespace Fcg.Identity.Api.Configurations;

public static class LoggingConfiguration
{
    public static void AddLoggingConfiguration(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        Log.Logger = new LoggerConfiguration()
            // Read base configuration from appsettings.json
            .ReadFrom.Configuration(configuration)
            // Additional custom enrichers for observability
            .Enrich.WithProperty("ApplicationName", "fcg-identity-api")
            .Enrich.WithProperty("ServiceName", "fcg-identity-api")
            .Enrich.WithProperty(
                "Environment",
                configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production"
            )
            .CreateLogger();

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(Log.Logger, dispose: true);
        });
    }
}
