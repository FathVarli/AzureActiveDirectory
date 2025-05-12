using System.Threading.Tasks;
using AzureExternalDirectory.Infrastructure.GraphService.Model;

namespace AzureExternalDirectory.Application.AuthService
{
    public interface IGraphAuthService
    {
        Task<AuthenticationResult> AuthenticateUserAsync(UserCredentialsDto credentials);
        Task<UserDto> GetAuthenticatedUserInfoAsync(UserCredentialsDto credentials);
        Task<bool> ValidateUserCredentialsAsync(UserCredentialsDto credentials);
        Task<AuthenticationResult> AuthenticateAndGetUserInfoAsync(UserCredentialsDto credentials);
    }
}