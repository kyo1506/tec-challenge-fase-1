using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Fcg.Identity.Api.Configurations;

public static class HealthChecksConfig
{
    public static void AddHealthChecksConfig(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddCheck(
                "Identity Service",
                () => HealthCheckResult.Healthy("Identity service is running")
            );
    }
}
