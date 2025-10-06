using System.Globalization;
using Asp.Versioning.ApiExplorer;
using Fcg.Identity.Api.Configurations;
using Fcg.Identity.Api.Middleware;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Serilog;

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
        configuration.ReadFrom.Configuration(context.Configuration).ReadFrom.Services(services)
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
