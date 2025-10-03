using Fcg.Identity.Client.Interfaces;
using Fcg.Identity.Client.Middleware;
using Fcg.Identity.Client.Services;
using Fcg.Identity.Client.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;

namespace Fcg.Identity.Client.Extensions;

/// <summary>
/// Extensões para configuração do cliente de identidade.
/// </summary>
public static class ServiceCollectionExtensions
{
    public const string AuthenticationScheme = "IdentityMicroservice";

    /// <summary>
    /// Adiciona o cliente de identidade aos serviços.
    /// </summary>
    /// <param name="services">Collection de serviços</param>
    /// <param name="identityServiceUrl">URL do microserviço de identidade</param>
    /// <returns>ServiceCollection para chaining</returns>
    public static IServiceCollection AddIdentityClient(
        this IServiceCollection services,
        string identityServiceUrl
    )
    {
        services.AddHttpClient<IIdentityClient, IdentityClient>(client =>
        {
            client.BaseAddress = new Uri(identityServiceUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Configure authentication
        services.AddAuthentication(AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, IdentityAuthenticationHandler>(AuthenticationScheme, null);

        return services;
    }

    /// <summary>
    /// Adiciona o cliente de identidade usando configuração do appsettings.json.
    /// </summary>
    /// <param name="services">Collection de serviços</param>
    /// <param name="configuration">Configuração da aplicação</param>
    /// <returns>ServiceCollection para chaining</returns>
    public static IServiceCollection AddIdentityClient(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var identityServiceUrl = configuration.GetConnectionString("IdentityService") 
            ?? configuration["IdentityService:BaseUrl"]
            ?? throw new InvalidOperationException("IdentityService BaseUrl não configurada");

        return services.AddIdentityClient(identityServiceUrl);
    }

    /// <summary>
    /// Adiciona o cliente de identidade com configuração customizada.
    /// </summary>
    /// <param name="services">Collection de serviços</param>
    /// <param name="configureClient">Ação para configurar o HttpClient</param>
    /// <returns>ServiceCollection para chaining</returns>
    public static IServiceCollection AddIdentityClient(
        this IServiceCollection services,
        Action<HttpClient> configureClient
    )
    {
        services.AddHttpClient<IIdentityClient, IdentityClient>(configureClient);
        
        // Configure authentication
        services.AddAuthentication(AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, IdentityAuthenticationHandler>(AuthenticationScheme, null);

        return services;
    }

    /// <summary>
    /// Adiciona o cliente de identidade com opções avançadas.
    /// </summary>
    /// <param name="services">Collection de serviços</param>
    /// <param name="configure">Ação para configurar as opções</param>
    /// <returns>ServiceCollection para chaining</returns>
    public static IServiceCollection AddIdentityClient(
        this IServiceCollection services,
        Action<IdentityClientOptions> configure
    )
    {
        var options = new IdentityClientOptions();
        configure(options);

        if (string.IsNullOrEmpty(options.BaseUrl))
            throw new InvalidOperationException("BaseUrl é obrigatória");

        services.AddHttpClient<IIdentityClient, IdentityClient>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = options.Timeout;
            
            foreach (var header in options.DefaultHeaders)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        });

        services.AddSingleton(options);
        
        // Configure authentication
        services.AddAuthentication(AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, IdentityAuthenticationHandler>(AuthenticationScheme, null);

        return services;
    }
}

/// <summary>
/// Opções de configuração para o cliente de identidade.
/// </summary>
public class IdentityClientOptions
{
    /// <summary>
    /// URL base do microserviço de identidade.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Timeout para requisições.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Headers padrão a serem enviados em todas as requisições.
    /// </summary>
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
    
    /// <summary>
    /// Habilitar retry automático em caso de falha.
    /// </summary>
    public bool EnableRetry { get; set; } = true;
    
    /// <summary>
    /// Número máximo de tentativas.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
}

/// <summary>
/// Extensões para configuração do middleware de autenticação.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adiciona o middleware de autenticação via microserviço de identidade.
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder para chaining</returns>
    public static IApplicationBuilder UseIdentityAuthentication(this IApplicationBuilder app)
    {
        return app.UseMiddleware<IdentityAuthenticationMiddleware>();
    }
}
