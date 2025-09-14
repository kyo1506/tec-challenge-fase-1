using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Events;
using Fcg.Identity.Api.Extensions;

namespace Fcg.Identity.Api.Configurations;

public static class ApiConfiguration
{
    public static void AddApiConfiguration(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.DefaultIgnoreCondition =
                    JsonIgnoreCondition.WhenWritingNull;
            });

        services
            .AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        services.AddCors(options =>
        {
            options.AddPolicy(
                "Development",
                builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
            );
        });
    }

    public static void UseApiConfig(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseCors("Development");
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseCors();
        }

        //TODO: Remover após implantar o deploy usando certificado SSL HTTPS
        // app.UseHsts();
        // app.UseHttpsRedirection();

        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (httpContext, elapsed, ex) => LogEventLevel.Information;
        });

        app.UseRouting();

        app.UseAuthentication();

        app.UseAuthorization();

        app.UseStaticFiles();

        app.UseExceptionHandler();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();

            endpoints.MapHealthChecks(
                "/health",
                new HealthCheckOptions
                {
                    ResponseWriter = async (context, report) =>
                    {
                        context.Response.ContentType = "application/json";
                        var options = new JsonSerializerOptions { WriteIndented = true };

                        var response = new
                        {
                            Status = report.Status.ToString(),
                            TotalDuration = report.TotalDuration.TotalMilliseconds,
                            Checks = report.Entries.Select(entry => new
                            {
                                Name = entry.Key,
                                Status = entry.Value.Status.ToString(),
                                entry.Value.Description,
                                Duration = entry.Value.Duration.TotalMilliseconds,
                                entry.Value.Data,
                            }),
                        };

                        await context.Response.WriteAsync(
                            JsonSerializer.Serialize(response, options)
                        );
                    },
                }
            );
        });
    }
}
