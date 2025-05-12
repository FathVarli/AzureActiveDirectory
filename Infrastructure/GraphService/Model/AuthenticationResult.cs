using System;
using System.Collections.Generic;

namespace AzureExternalDirectory.Infrastructure.GraphService.Model
{
    public class AuthenticationResult
    {
        public bool IsSuccess { get; set; }
        public string Username { get; set; } = string.Empty;
        public string UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ErrorCode { get; set; }
        public string ErrorDetails { get; set; }
        public DateTime AuthenticationTime { get; set; }
        public UserDto UserInfo { get; set; }
        public List<GroupDto> UserGroups { get; set; } = new List<GroupDto>();
    }
}