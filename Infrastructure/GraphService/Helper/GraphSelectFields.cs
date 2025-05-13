using AzureExternalDirectory.Infrastructure.GraphService.Model;

namespace AzureExternalDirectory.Infrastructure.GraphService.Helper
{
    /// <summary>
    /// Microsoft Graph API için select ve expand alanlarını içeren sabitler
    /// </summary>
    public static class GraphSelectFields
    {
        /// <summary>
        /// User entitysi için select alanları
        /// </summary>
        public static class User
        {
            // Basic User Fields
            public const string Id = "id";
            public const string DisplayName = "displayName";
            public const string Mail = "mail";
            public const string UserPrincipalName = "userPrincipalName";
            public const string MailNickname = "mailNickname";
            public const string PreferredLanguage = "preferredLanguage";
            public const string Department = "department";
            public const string JobTitle = "jobTitle";
            public const string CompanyName = "companyName";
            public const string EmployeeId = "employeeId";
            public const string OfficeLocation = "officeLocation";
            
            // Contact Information
            public const string MobilePhone = "mobilePhone";
            public const string BusinessPhones = "businessPhones";
            public const string FaxNumber = "faxNumber";
            
            // Status & Settings
            public const string AccountEnabled = "accountEnabled";
            public const string OnPremisesSyncEnabled = "onPremisesSyncEnabled";
            public const string OnPremisesImmutableId = "onPremisesImmutalId";
            public const string CreatedDateTime = "createdDateTime";
            public const string LastPasswordChangeDateTime = "lastPasswordChangeDateTime";
            
            // Address Information
            public const string StreetAddress = "streetAddress";
            public const string City = "city";
            public const string State = "state";
            public const string PostalCode = "postalCode";
            public const string Country = "country";
            
            // Personal Information
            public const string AgeGroup = "ageGroup";
            public const string Birthday = "birthday";
            
            // License Information
            public const string AssignedLicenses = "assignedLicenses";
            public const string AssignedPlans = "assignedPlans";
            
            // Additional Properties
            public const string UsageLocation = "usageLocation";
            public const string UserType = "userType";
            public const string ProxyAddresses = "proxyAddresses";
            
            // Predefined collections
            public static readonly string[] Basic = new[]
            {
                Id, DisplayName, Mail, UserPrincipalName
            };
            
            public static readonly string[] Default = new[]
            {
                Id, DisplayName, Mail, UserPrincipalName, JobTitle, 
                Department, OfficeLocation, MobilePhone, BusinessPhones, AccountEnabled
            };
            
            public static readonly string[] Extended = new[]
            {
                Id, DisplayName, Mail, UserPrincipalName, JobTitle, Department, 
                OfficeLocation, MobilePhone, BusinessPhones, AccountEnabled,
                EmployeeId, CompanyName, MailNickname, PreferredLanguage, 
                CreatedDateTime, UserType, UsageLocation
            };
            
            public static readonly string[] Contact = new[]
            {
                Id, DisplayName, Mail, UserPrincipalName, MobilePhone, 
                BusinessPhones, FaxNumber, StreetAddress, City, State, 
                PostalCode, Country
            };
            
            public static readonly string[] All = new[]
            {
                Id, DisplayName, Mail, UserPrincipalName, MailNickname, PreferredLanguage,
                Department, JobTitle, CompanyName, EmployeeId, OfficeLocation,
                MobilePhone, BusinessPhones, FaxNumber, AccountEnabled, OnPremisesSyncEnabled,
                OnPremisesImmutableId, CreatedDateTime, StreetAddress, City, State, 
                PostalCode, Country, UsageLocation, UserType, ProxyAddresses
            };
            
            // Sadece okuma permission'ı gereken safe alanlar
            public static readonly string[] AllSafe = new[]
            {
                Id, DisplayName, Mail, UserPrincipalName, MailNickname, PreferredLanguage,
                Department, JobTitle, CompanyName, EmployeeId, OfficeLocation,
                MobilePhone, BusinessPhones, AccountEnabled, CreatedDateTime,
                StreetAddress, City, State, PostalCode, Country, UsageLocation, UserType
            };
        }
        
        /// <summary>
        /// Group entitysi için select alanları
        /// </summary>
        public static class Group
        {
            // Basic Group Fields
            public const string Id = "id";
            public const string DisplayName = "displayName";
            public const string Mail = "mail";
            public const string MailNickname = "mailNickname";
            public const string MailEnabled = "mailEnabled";
            public const string SecurityEnabled = "securityEnabled";
            public const string Description = "description";
            public const string Classification = "classification";
            public const string CreatedDateTime = "createdDateTime";
            public const string DeletedDateTime = "deletedDateTime";
            public const string ExpirationDateTime = "expirationDateTime";
            public const string IsAssignableToRole = "isAssignableToRole";
            public const string OnPremisesSyncEnabled = "onPremisesSyncEnabled";
            public const string MembershipRule = "membershipRule";
            public const string MembershipRuleProcessingState = "membershipRuleProcessingState";
            public const string GroupTypes = "groupTypes";
            public const string ProxyAddresses = "proxyAddresses";
            public const string RenewedDateTime = "renewedDateTime";
            public const string Visibility = "visibility";
            public const string AllowExternalSenders = "allowExternalSenders";
            public const string AutoSubscribeNewMembers = "autoSubscribeNewMembers";
            public const string HideFromAddressLists = "hideFromAddressLists";
            public const string HideFromOutlookClients = "hideFromOutlookClients";
            public const string UnseenCount = "unseenCount";
            
            // Member/Owner counts
            public const string MemberCount = "memberCount";
            public const string OwnerCount = "ownerCount";
            
            // Predefined collections
            public static readonly string[] Basic = new[]
            {
                Id, DisplayName, Mail
            };
            
            public static readonly string[] Default = new[]
            {
                Id, DisplayName, Mail, MailNickname, Description, GroupTypes, 
                SecurityEnabled, MailEnabled
            };
            
            public static readonly string[] Extended = new[]
            {
                Id, DisplayName, Mail, MailNickname, Description, GroupTypes,
                SecurityEnabled, MailEnabled, Classification, CreatedDateTime,
                Visibility, IsAssignableToRole, OnPremisesSyncEnabled,
                MembershipRule, MembershipRuleProcessingState
            };
            
            public static readonly string[] Security = new[]
            {
                Id, DisplayName, Description, SecurityEnabled, IsAssignableToRole,
                MembershipRule, MembershipRuleProcessingState
            };
            
            public static readonly string[] Teams = new[]
            {
                Id, DisplayName, Mail, MailNickname, Description, GroupTypes,
                MailEnabled, Visibility, Classification, UnseenCount
            };
            
            public static readonly string[] All = new[]
            {
                Id, DisplayName, Mail, MailNickname, MailEnabled, SecurityEnabled,
                Description, Classification, CreatedDateTime, GroupTypes, 
                Visibility, IsAssignableToRole, OnPremisesSyncEnabled,
                MembershipRule, MembershipRuleProcessingState, ProxyAddresses
                // DeletedDateTime, ExpirationDateTime, RenewedDateTime, AllowExternalSenders,
                // AutoSubscribeNewMembers, HideFromAddressLists, HideFromOutlookClients, UnseenCount kaldırıldı
                // Bu alanlar özel permission gerektirebilir
            };
            
            // Sadece okuma permission'ı gereken safe alanlar  
            public static readonly string[] AllSafe = new[]
            {
                Id, DisplayName, Mail, MailNickname, MailEnabled, SecurityEnabled,
                Description, Classification, CreatedDateTime, GroupTypes, 
                Visibility, IsAssignableToRole
            };
        }
        
        /// <summary>
        /// Permission entitysi için select alanları
        /// </summary>
        public static class Permission
        {
            public const string Id = "id";
            public const string GrantedToIdentities = "grantedToIdentities";
            public const string Roles = "roles";
            public const string GrantedTo = "grantedTo";
            public const string Invitation = "invitation";
            public const string InheritedFrom = "inheritedFrom";
            public const string Link = "link";
            public const string ShareId = "shareId";
            
            public static readonly string[] Default = new[]
            {
                Id, GrantedToIdentities, Roles, GrantedTo
            };
        }
        
        /// <summary>
        /// Application entitysi için select alanları
        /// </summary>
        public static class Application
        {
            public const string Id = "id";
            public const string DisplayName = "displayName";
            public const string AppId = "appId";
            public const string IdentifierUris = "identifierUris";
            public const string Web = "web";
            public const string Spa = "spa";
            public const string PublicClient = "publicClient";
            public const string RequiredResourceAccess = "requiredResourceAccess";
            public const string KeyCredentials = "keyCredentials";
            public const string PasswordCredentials = "passwordCredentials";
            public const string Tags = "tags";
            public const string CreatedDateTime = "createdDateTime";
            public const string DeletedDateTime = "deletedDateTime";
            
            public static readonly string[] Default = new[]
            {
                Id, DisplayName, AppId, IdentifierUris
            };
        }
    }
    
    /// <summary>
    /// Microsoft Graph API için expand alanlarını içeren sabitler
    /// </summary>
    public static class GraphExpandFields
    {
        /// <summary>
        /// User entitysi için expand alanları
        /// </summary>
        public static class User
        {
            public const string MemberOf = "memberOf";
            public const string OwnedObjects = "ownedObjects";
            public const string Manager = "manager";
            public const string DirectReports = "directReports";
            public const string CreatedObjects = "createdObjects";
            public const string RegisteredDevices = "registeredDevices";
            public const string OwnedDevices = "ownedDevices";
            public const string Licenses = "licenses";
            public const string Activities = "activities";
            public const string Calendar = "calendar";
            public const string CalendarGroups = "calendarGroups";
            public const string Calendars = "calendars";
            public const string Events = "events";
            public const string People = "people";
            public const string Contacts = "contacts";
            public const string ContactFolders = "contactFolders";
            public const string InferenceClassification = "inferenceClassification";
            
            // Expand with select combinations
            public static string ManagerWithDetails => $"{Manager}($select={GraphSelectFields.User.Id},{GraphSelectFields.User.DisplayName},{GraphSelectFields.User.Mail})";
            public static string MemberOfWithBasic => $"{MemberOf}($select={GraphSelectFields.Group.Id},{GraphSelectFields.Group.DisplayName},{GraphSelectFields.Group.MailNickname})";
            public static string DirectReportsWithBasic => $"{DirectReports}($select={GraphSelectFields.User.Id},{GraphSelectFields.User.DisplayName},{GraphSelectFields.User.JobTitle})";
        }
        
        /// <summary>
        /// Group entitysi için expand alanları
        /// </summary>
        public static class Group
        {
            public const string Members = "members";
            public const string Owners = "owners";
            public const string MemberOf = "memberOf";
            public const string PermissionGrants = "permissionGrants";
            public const string AcceptedSenders = "acceptedSenders";
            public const string RejectedSenders = "rejectedSenders";
            public const string Settings = "settings";
            public const string Extensions = "extensions";
            public const string Sites = "sites";
            public const string Drives = "drives";
            public const string Calendar = "calendar";
            public const string Conversations = "conversations";
            public const string Threads = "threads";
            
            // Expand with select combinations
            public static string MembersWithBasic => $"{Members}($select={GraphSelectFields.User.Id},{GraphSelectFields.User.DisplayName},{GraphSelectFields.User.Mail})";
            public static string OwnersWithBasic => $"{Owners}($select={GraphSelectFields.User.Id},{GraphSelectFields.User.DisplayName},{GraphSelectFields.User.Mail})";
            public static string MemberOfWithBasic => $"{MemberOf}($select={GraphSelectFields.Group.Id},{GraphSelectFields.Group.DisplayName},{GraphSelectFields.Group.MailNickname})";
        }
    }
    
    /// <summary>
    /// Ortak kullanılan OData query parametreleri
    /// </summary>
    public static class ODataQueryParameters
    {
        // Top values
        public const int DefaultPageSize = 25;
        public const int MaxPageSize = 999;
        public const int SmallPageSize = 10;
        public const int LargePageSize = 100;
        
        // Common filters
        public const string ActiveUsersFilter = "accountEnabled eq true";
        public const string DisabledUsersFilter = "accountEnabled eq false";
        public const string SecurityGroupsFilter = "securityEnabled eq true";
        public const string MailEnabledGroupsFilter = "mailEnabled eq true";
        public const string Teams365GroupsFilter = "groupTypes/any(c:c eq 'Unified')";
        
        // Common orderBy
        public const string OrderByDisplayName = "displayName";
        public const string OrderByCreatedDateTime = "createdDateTime desc";
        public const string OrderByMail = "mail";
    }
    
    /// <summary>
    /// Yaygın kullanılan kombinasyonlar
    /// </summary>
    public static class CommonGraphQueries
    {
        /// <summary>
        /// User için tipik sorgu kombinasyonları
        /// </summary>
        public static class User
        {
            public static GraphQueryOptions Basic => new GraphQueryOptions
            {
                Select = GraphSelectFields.User.Basic
            };
            
            public static GraphQueryOptions DefaultWithManager => new GraphQueryOptions
            {
                Select = GraphSelectFields.User.Default,
                Expand = new[] { GraphExpandFields.User.ManagerWithDetails }
            };
            
            public static GraphQueryOptions ExtendedWithGroups => new GraphQueryOptions
            {
                Select = GraphSelectFields.User.Extended,
                Expand = new[] { GraphExpandFields.User.MemberOfWithBasic }
            };
        }
        
        /// <summary>
        /// Group için tipik sorgu kombinasyonları
        /// </summary>
        public static class Group
        {
            public static GraphQueryOptions Basic => new GraphQueryOptions
            {
                Select = GraphSelectFields.Group.Basic
            };
            
            public static GraphQueryOptions DefaultWithMembers => new GraphQueryOptions
            {
                Select = GraphSelectFields.Group.Default,
                Expand = new[] { GraphExpandFields.Group.MembersWithBasic }
            };
            
            public static GraphQueryOptions ExtendedWithOwnersAndMembers => new GraphQueryOptions
            {
                Select = GraphSelectFields.Group.Extended,
                Expand = new[] { GraphExpandFields.Group.OwnersWithBasic, GraphExpandFields.Group.MembersWithBasic }
            };
        }
    }
}