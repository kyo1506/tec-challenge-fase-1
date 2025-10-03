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
            // Set the minimum level to Debug to capture everything during diagnostics
            .MinimumLevel.Debug()
            // Override noisy sources to keep the log clean
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            // This is the most important override for your 401 error!
            .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ApplicationName", "fcg-identity-api")
            // Write to the console for real-time feedback
            .WriteTo.Console()
            .CreateLogger();

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(Log.Logger, dispose: true);
        });
    }
}
