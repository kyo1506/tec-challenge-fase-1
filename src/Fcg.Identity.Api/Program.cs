using System.Globalization;
using Asp.Versioning.ApiExplorer;
using Fcg.Identity.Api.Configurations;
using Fcg.Identity.Api.Middlewares;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

IdentityModelEventSource.ShowPII = builder.Environment.IsDevelopment();

builder
    .Configuration.SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddIdentityConfiguration(builder.Configuration);
builder.Services.AddApiConfiguration(builder.Configuration);
builder.Services.AddHealthChecksConfig();
builder.Services.AddScalarConfiguration();
builder.Services.AddLoggingConfiguration(builder.Configuration);
builder.Services.AddOpenTelemetryConfiguration(builder.Configuration, builder.Environment);
builder.Services.ResolveDependencies();
builder.Services.AddLocalization();

// Adicionar Serilog para o host (necessário para UseSerilogRequestLogging)
builder.Host.UseSerilog();

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

app.UseRequestLocalization(
    app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value
);

var apiVersionDescriptionProvider =
    app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

app.UseScalarConfig();

app.Run();
