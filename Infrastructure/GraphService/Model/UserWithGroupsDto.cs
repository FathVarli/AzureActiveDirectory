using System.Collections.Generic;

namespace AzureExternalDirectory.Infrastructure.GraphService.Model
{
    public class UserWithGroupsDto
    {
        public UserDto User { get; set; } = new UserDto();
        public List<GroupDto> Groups { get; set; } = new List<GroupDto>();
    }
}