using Serilog;

namespace Fcg.Identity.Api.Configurations;

public static class LoggingConfiguration
{
    public static void AddLoggingConfiguration(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        var serviceName = configuration["OTEL_SERVICE_NAME"] ?? "fcg-identity-api";
        var serviceVersion =
            configuration["OTEL_RESOURCE_ATTRIBUTES"]
                ?.Split(',')
                .FirstOrDefault(x => x.StartsWith("service.version="))
                ?.Split('=')[1]
            ?? "1.0.0";
        var deploymentEnvironment =
            configuration["OTEL_RESOURCE_ATTRIBUTES"]
                ?.Split(',')
                .FirstOrDefault(x => x.StartsWith("deployment.environment="))
                ?.Split('=')[1]
            ?? configuration["ASPNETCORE_ENVIRONMENT"]
            ?? "Production";

        var loggerConfig = new LoggerConfiguration()
            // Read base configuration from appsettings.json
            .ReadFrom.Configuration(configuration)
            // Additional custom enrichers for observability
            .Enrich.WithProperty("ApplicationName", serviceName)
            .Enrich.WithProperty("ServiceName", serviceName)
            .Enrich.WithProperty("Environment", deploymentEnvironment);

        // Adiciona o sink OpenTelemetry se o endpoint estiver configurado
        if (!string.IsNullOrEmpty(otlpEndpoint))
        {
            loggerConfig.WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = otlpEndpoint;
                options.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = serviceName,
                    ["service.version"] = serviceVersion,
                    ["deployment.environment"] = deploymentEnvironment,
                };
            });
        }

        Log.Logger = loggerConfig.CreateLogger();

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(Log.Logger, dispose: true);
        });
    }
}
