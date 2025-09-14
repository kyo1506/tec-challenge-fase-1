using System.Net.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Fcg.Identity.Api.Configurations;

public static class HealthChecksConfig
{
    private static readonly string[] PostgresTags = ["postgres-server"];
    private static readonly string[] MailTags = ["email-server"];

    public static void AddHealthChecksConfig(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services
            .AddHealthChecks()
            .AddNpgSql(
                connectionString ?? throw new InvalidDataException(),
                "SELECT 1",
                name: "Database",
                failureStatus: HealthStatus.Degraded,
                tags: PostgresTags
            );

        // .AddCheck<MailHealthCheck>(
        //     "Mail Server",
        //     HealthStatus.Unhealthy,
        //     MailTags
        // );
    }
}
