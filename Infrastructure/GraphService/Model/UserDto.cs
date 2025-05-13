using System.Collections.Generic;

namespace AzureExternalDirectory.Infrastructure.GraphService.Model
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Mail { get; set; } = string.Empty;
        public string UserPrincipalName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string OfficeLocation { get; set; } = string.Empty;
        public string MobilePhone { get; set; } = string.Empty;
        public List<string> BusinessPhones { get; set; } = new List<string>();
        public bool AccountEnabled { get; set; } = false;
        public List<GroupDto> Groups { get; set; } = new List<GroupDto>();
        public List<string> GroupIds { get; set; } = new List<string>();
    }
}