using System.Collections.Generic;
using System.Threading.Tasks;
using AzureExternalDirectory.Infrastructure.GraphService.Model;

namespace AzureExternalDirectory.Application.UserService
{
    public interface IUserGraphService
    {
        Task<List<UserDto>> GetAllUsersAsync(UserFilterOptions filter = null);
        Task<List<UserWithGroupsDto>> GetUsersWithGroupsAsync(UserFilterOptions filter = null);
        Task<UserDto> GetUserByIdAsync(string userId);
        Task<List<UserDto>> GetUsersByIdsAsync(List<string> userIds);
        Task<List<UserDto>> SearchUsersAsync(string searchTerm, int? top = null);
        Task<List<GroupDto>> GetUserGroupsAsync(string userId);
    }
}