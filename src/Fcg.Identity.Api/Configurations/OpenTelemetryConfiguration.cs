using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Fcg.Identity.Api.Configurations;

public static class OpenTelemetryConfiguration
{
    public static IServiceCollection AddOpenTelemetryConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment
    )
    {
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
            ?? environment.EnvironmentName;

        var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

        // Se não houver endpoint OTLP configurado, não adiciona OpenTelemetry
        if (string.IsNullOrEmpty(otlpEndpoint))
        {
            return services;
        }

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource
                    .AddService(
                        serviceName: serviceName,
                        serviceVersion: serviceVersion,
                        serviceInstanceId: Environment.MachineName
                    )
                    .AddAttributes(
                        new Dictionary<string, object>
                        {
                            ["deployment.environment"] = deploymentEnvironment,
                            ["host.name"] = Environment.MachineName,
                        }
                    )
            )
            .WithTracing(tracing =>
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = (httpContext) =>
                        {
                            return !httpContext.Request.Path.StartsWithSegments("/health");
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddOtlpExporter()
            )
            .WithMetrics(metrics =>
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter()
            );

        return services;
    }
}
