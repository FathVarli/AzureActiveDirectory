using System.Collections.Generic;

namespace AzureExternalDirectory.Infrastructure.GraphService.Model
{
    public class GroupDto
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Mail { get; set; } = string.Empty;
        public string MailNickname { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> GroupTypes { get; set; } = new List<string>();
        public bool SecurityEnabled { get; set; }
        public bool MailEnabled { get; set; }
    }
}