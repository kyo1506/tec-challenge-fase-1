using System.Globalization;
using Asp.Versioning.ApiExplorer;
using Fcg.Identity.Api.Configurations;
using Fcg.Identity.Api.Middleware;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Serilog;
using Serilog.Sinks.Elasticsearch;

var builder = WebApplication.CreateBuilder(args);

NewRelic.Api.Agent.NewRelic.SetApplicationName(
    builder.Configuration["NEW_RELIC_APP_NAME"] ?? "FCG Identity API",
    builder.Configuration["NEW_RELIC_LICENSE_KEY"]
        ?? Environment.GetEnvironmentVariable("NEW_RELIC_LICENSE_KEY")
);

IdentityModelEventSource.ShowPII = true;

builder
    .Configuration.SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddIdentityConfiguration(builder.Configuration);
builder.Services.AddApiConfiguration(builder.Configuration);
builder.Services.AddSwaggerConfiguration(builder.Configuration);
builder.Services.AddHealthChecksConfig(builder.Configuration);
builder.Services.AddLoggingConfiguration(builder.Configuration);
builder.Services.ResolveDependencies();
builder.Services.AddLocalization();

builder.Host.UseSerilog(
    (context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("ServiceName", "fcg-identity-api")
            .Enrich.WithProperty("Application", "FCG Identity API")
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} "
                    + "{NewLine}{Exception}"
                    + "| RequestId: {RequestId} | UserId: {UserId} | Username: {Username} | SessionId: {SessionId}"
            )
            .WriteTo.Elasticsearch(
                new ElasticsearchSinkOptions(
                    new Uri(context.Configuration["Elasticsearch:Uri"] ?? "https://localhost:9200")
                )
                {
                    IndexFormat = "fcg-identity-logs-{0:yyyy.MM.dd}",
                    TypeName = null,
                    AutoRegisterTemplate = true,
                    OverwriteTemplate = true,
                    NumberOfShards = 1,
                    NumberOfReplicas = 1,
                    ModifyConnectionSettings = x =>
                        x.ApiKeyAuthentication(
                            "DjnAtZkBb1NdWTIe6DAt",
                            context.Configuration["Elasticsearch:ApiKey"] ?? ""
                        ),
                }
            )
);

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new List<CultureInfo> { new("pt-BR"), new("en-US") };

    options.SetDefaultCulture("pt-BR");
    options.DefaultRequestCulture = new RequestCulture("pt-BR");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.ApplyCurrentCultureToResponseHeaders = true;
});

var app = builder.Build();

app.UseApiConfig(app.Environment);

app.UseLogContext();

var apiVersionDescriptionProvider =
    app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

app.UseSwaggerConfig(apiVersionDescriptionProvider, app.Configuration);

app.UseRequestLocalization(
    app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value
);

app.Run();
