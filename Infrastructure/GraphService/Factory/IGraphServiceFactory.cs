using System.Threading.Tasks;
using AzureExternalDirectory.Domain;
using Microsoft.Graph;

namespace AzureExternalDirectory.Infrastructure.GraphService.Factory
{
    public interface IGraphServiceFactory
    {
        /// <summary>
        /// Sistem bazlı Graph client oluşturur (Client Credentials Flow)
        /// </summary>
        /// <param name="azureAdConfiguration">Azure AD konfigürasyonu</param>
        /// <returns>Graph Service Client</returns>
        Task<GraphServiceClient> CreateSystemClientAsync(AzureAdConfiguration azureAdConfiguration);
        
        /// <summary>
        /// Kullanıcı adı ve şifre ile Graph client oluşturur (ROPC - Güvenlik riski)
        /// </summary>
        /// <param name="username">Kullanıcı adı</param>
        /// <param name="password">Şifre</param>
        /// <param name="azureAdConfiguration">Azure AD konfigürasyonu</param>
        /// <returns>Graph Service Client</returns>
        [System.Obsolete("ROPC flow güvenlik riski oluşturur. CreateUserClientWithCodeAsync kullanın.", false)]
        Task<GraphServiceClient> CreateUserClientAsync(string username, string password, AzureAdConfiguration azureAdConfiguration);
        
        /// <summary>
        /// Graph client'ı dispose eder
        /// </summary>
        /// <param name="client">Dispose edilecek client</param>
        void DisposeClient(GraphServiceClient client);
    }
}