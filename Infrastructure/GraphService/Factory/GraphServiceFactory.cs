using System;
using System.Threading.Tasks;
using Azure.Identity;
using AzureExternalDirectory.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace AzureExternalDirectory.Infrastructure.GraphService.Factory
{
    public class GraphServiceFactory : IGraphServiceFactory
    {
        private readonly ILogger<GraphServiceFactory> _logger;

        public GraphServiceFactory(ILogger<GraphServiceFactory> logger)
        {
            _logger = logger;
        }

        // 1. System client
        public async Task<GraphServiceClient> CreateSystemClientAsync(AzureAdConfiguration azureAdConfiguration)
        {
            try
            {
                _logger.LogInformation("Sistem Graph client oluşturuluyor...");

                var credential = new ClientSecretCredential(
                    azureAdConfiguration.TenantId,
                    azureAdConfiguration.ClientId,
                    azureAdConfiguration.ClientSecret
                );

                var scopes = new[] { "https://graph.microsoft.com/.default" };
                var client = new GraphServiceClient(credential, scopes);

                // Bağlantıyı test et - daha güvenilir yöntemler
                await TestConnectionAsync(client);

                _logger.LogInformation("Sistem Graph client başarıyla oluşturuldu");
                return client;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sistem Graph client oluşturulamadı");
                throw new InvalidOperationException("Graph Service Client oluşturulamadı", ex);
            }
        }

        // 2. User client (ROPC)
        public async Task<GraphServiceClient> CreateUserClientAsync(string username, string password, AzureAdConfiguration azureAdConfiguration)
        {
            try
            {
                _logger.LogWarning("ROPC flow kullanılıyor - güvenlik riski var!");

                var credential = new UsernamePasswordCredential(
                    username,
                    password,
                    azureAdConfiguration.TenantId,
                    azureAdConfiguration.ClientId
                );

                var scopes = new[] { 
                    "User.Read",
                    "Mail.Read"
                };

                var client = new GraphServiceClient(credential, scopes);

                // User client için test
                await TestUserConnectionAsync(client);

                _logger.LogInformation("Kullanıcı Graph client başarıyla oluşturuldu");
                return client;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı Graph client oluşturulamadı: {Username}", username);
                throw new UnauthorizedAccessException("Kullanıcı kimlik doğrulaması başarısız", ex);
            }
        }
        

        // Bağlantı test metodları
        private async Task TestConnectionAsync(GraphServiceClient client)
        {
            try
            {
                _logger.LogDebug("Graph bağlantı testi başlıyor...");

                // Test 1: Organization bilgisini al
                var organizations = await client.Organization.GetAsync();
                
                if (organizations?.Value is { Count: > 0 })
                {
                    _logger.LogDebug("Organization test başarılı");
                    return;
                }

                // Test 2: Applications ile test et (system permissions gerekli)
                try
                {
                    var applications = await client.Applications.GetAsync(config =>
                    {
                        config.QueryParameters.Top = 1;
                    });
                    _logger.LogDebug("Applications test başarılı");
                    return;
                }
                catch (Exception appEx)
                {
                    _logger.LogDebug(appEx, "Applications test başarısız, Users ile deneyeceğim");
                }

                // Test 3: Users ile test et
                try
                {
                    var users = await client.Users.GetAsync(config =>
                    {
                        config.QueryParameters.Top = 1;
                        config.QueryParameters.Select = new[] { "displayName", "id" };
                    });
                    _logger.LogDebug("Users test başarılı");
                    return;
                }
                catch (Exception userEx)
                {
                    _logger.LogWarning(userEx, "Users test de başarısız");
                }

                // Eğer hiçbiri çalışmazsa genel hata
                throw new InvalidOperationException("Hiçbir Graph API testi başarılı olmadı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Graph bağlantı testi başarısız");
                throw;
            }
        }

        private async Task TestUserConnectionAsync(GraphServiceClient client)
        {
            try
            {
                _logger.LogDebug("User Graph bağlantı testi başlıyor...");

                // User context'de sadece Me endpoint'ini test et
                var me = await client.Me.GetAsync();
                
                _logger.LogDebug("User bağlantı testi başarılı: {DisplayName}", me?.DisplayName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User Graph bağlantı testi başarısız");
                throw;
            }
        }

        public void DisposeClient(GraphServiceClient client)
        {
            try
            {
                client?.Dispose();
                _logger.LogDebug("GraphServiceClient dispose edildi");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GraphServiceClient dispose edilirken hata oluştu");
            }
        }
    }
}