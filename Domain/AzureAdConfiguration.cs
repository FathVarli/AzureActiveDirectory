namespace AzureExternalDirectory.Domain
{
    public class AzureAdConfiguration
    {
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public bool IsActive { get; set; } = true;
        public string RedirectUri { get; set; } // Authorization Code Flow için
        public string Instance { get; set; } = "https://login.microsoftonline.com/";
        
        // Validasyon metodu
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(TenantId) && 
                   !string.IsNullOrEmpty(ClientId);
        }
        
        // System client için validasyon
        public bool IsValidForSystemClient()
        {
            return IsValid() && !string.IsNullOrEmpty(ClientSecret);
        }
        
        // Interactive client için validasyon
        public bool IsValidForInteractiveClient()
        {
            return IsValid() && !string.IsNullOrEmpty(RedirectUri);
        }
    
    }
}