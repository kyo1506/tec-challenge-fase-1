using NewRelic.LogEnrichers.Serilog;
using NpgsqlTypes;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.PostgreSQL;

namespace Fcg.Identity.Api.Configurations;

public static class LoggingConfiguration
{
    public static void AddLoggingConfiguration(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var columnWriters = new Dictionary<string, ColumnWriterBase>
        {
            { "Message", new RenderedMessageColumnWriter(NpgsqlDbType.Text) },
            { "MessageTemplate", new MessageTemplateColumnWriter(NpgsqlDbType.Text) },
            { "Level", new LevelColumnWriter(true, NpgsqlDbType.Varchar) },
            { "TimeStamp", new TimestampColumnWriter(NpgsqlDbType.TimestampTz) },
            { "Exception", new ExceptionColumnWriter(NpgsqlDbType.Text) },
            { "Properties", new PropertiesColumnWriter(NpgsqlDbType.Jsonb) },
            {
                "ApplicationName",
                new SinglePropertyColumnWriter("ApplicationName", PropertyWriteMethod.Raw)
            },
        };

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("HealthChecks", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ApplicationName", "tech-challenge-api")
            .Enrich.WithThreadId()
            .Enrich.WithProcessId()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithNewRelicLogsInContext()
            .WriteTo.Console()
            .WriteTo.File(
                path: "logs/log-.json",
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: 7,
                formatter: new NewRelicFormatter()
            )
            .WriteTo.PostgreSQL(
                connectionString: connectionString,
                tableName: "Log",
                needAutoCreateTable: false,
                respectCase: true
            )
            .Filter.ByExcluding(logEvent => logEvent.RenderMessage().Contains("HTTP"))
            .CreateLogger();

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(Log.Logger, true);
        });
    }
}
