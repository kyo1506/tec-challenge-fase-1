using NewRelic.LogEnrichers.Serilog;
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
            // Add New Relic enricher for APM correlation
            .Enrich.WithNewRelicLogsInContext()
            // Additional custom enrichers
            .Enrich.WithProperty("ApplicationName", "fcg-identity-api")
            .Enrich.WithProperty("ServiceName", "fcg-identity-api")
            .CreateLogger();

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(Log.Logger, dispose: true);
        });
    }
}
