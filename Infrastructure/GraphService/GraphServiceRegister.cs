using AzureExternalDirectory.Domain;
using AzureExternalDirectory.Infrastructure.GraphService.Factory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AzureExternalDirectory.Infrastructure.GraphService
{
    public static class GraphServiceRegister
    {
            /// <summary>
            /// Microsoft Graph servislerini DI container'a kaydeder
            /// </summary>
            /// <param name="services">Service collection</param>
            /// <param name="configuration">Configuration</param>
            /// <returns>Service collection</returns>
            public static IServiceCollection AddGraphServices(this IServiceCollection services, IConfiguration configuration)
            {
                // Azure AD konfigürasyonunu kaydet
                services.Configure<AzureAdConfiguration>(configuration.GetSection("AzureAd"));
                

                // Factory'leri singleton olarak kaydet
                services.AddSingleton<GraphServiceFactory>();
                services.AddSingleton<IGraphServiceFactory>(provider => provider.GetRequiredService<GraphServiceFactory>());
                

                // HttpClient konfigürasyonu
                services.AddHttpClient();

                return services;
            }
    }
}