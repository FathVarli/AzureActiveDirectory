using System.Collections.Generic;
using System.Threading.Tasks;
using AzureExternalDirectory.Infrastructure.GraphService.Model;

namespace AzureExternalDirectory.Application.GroupService
{
    public interface IGroupGraphService
    {
        Task<List<GroupDto>> GetAllGroupsAsync(GroupFilterOptions filter = null);
        Task<GroupDto> GetGroupByIdAsync(string groupId);
        Task<List<GroupDto>> GetGroupsByIdsAsync(List<string> groupIds);
        Task<List<GroupDto>> SearchGroupsAsync(string searchTerm, int? top = null);
        Task<List<UserDto>> GetGroupMembersAsync(string groupId);
        Task<Dictionary<string, List<UserDto>>> GetMultipleGroupMembersAsync(List<string> groupIds);
        Task<List<UserDto>> GetGroupOwnersAsync(string groupId);
        Task<int> GetGroupMemberCountAsync(string groupId);
        Task<bool> IsUserGroupMemberAsync(string groupId, string userId);
        Task<List<GroupDto>> GetSubGroupsAsync(string groupId);
        Task<List<GroupDto>> GetParentGroupsAsync(string groupId);
    }
}