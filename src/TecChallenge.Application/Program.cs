using System.Globalization;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Serilog;
using TecChallenge.Application.Configurations;
using TecChallenge.Data.Contexts;

var builder = WebApplication.CreateBuilder(args);

builder
    .Configuration.SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", true, true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true)
    .AddEnvironmentVariables();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options
        .UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
        )
        .ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning));
});

builder.Services.AddIdentityConfiguration(builder.Configuration);
builder.Services.AddApiConfiguration(builder.Configuration);
builder.Services.AddSwaggerConfiguration();
builder.Services.AddHealthChecksConfig(builder.Configuration);
builder.Services.AddLoggingConfiguration(builder.Configuration);
builder.Services.ResolveDependencies();
builder.Services.AddLocalization();

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

if (builder.Environment.IsProduction()) builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

app.UseApiConfig(app.Environment);

if (app.Environment.IsDevelopment())
{
    var apiVersionDescriptionProvider =
        app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

    app.UseSwaggerConfig(apiVersionDescriptionProvider);

    await app.InitializeIdentityDatabase();
}

app.UseRequestLocalization(
    app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value
);

app.Run();