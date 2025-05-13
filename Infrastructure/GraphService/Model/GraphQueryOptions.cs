using System.Linq;
using AzureExternalDirectory.Infrastructure.GraphService.Helper;

namespace AzureExternalDirectory.Infrastructure.GraphService.Model
{
    /// <summary>
    /// Select ve Expand parametrelerini tutan yardımcı class
    /// </summary>
    public class GraphQueryOptions
    {
        public string[] Select { get; set; }
        public string[] Expand { get; set; }
        
        // Default constructor
        public GraphQueryOptions() { }
        
        // Constructor with select fields
        public GraphQueryOptions(string[] select, string[] expand = null)
        {
            Select = select;
            Expand = expand;
        }
        
        /// <summary>
        /// User select alanlarını veya default değerleri döndürür
        /// </summary>
        public string[] GetUserSelectOrDefault()
        {
            return Select ?? GraphSelectFields.User.Default;
        }
        
        /// <summary>
        /// Group select alanlarını veya default değerleri döndürür
        /// </summary>
        public string[] GetGroupSelectOrDefault()
        {
            return Select ?? GraphSelectFields.Group.Default;
        }
        
        /// <summary>
        /// Belirtilen select alanlarını veya varsayılan alanları döndürür
        /// </summary>
        public string[] GetSelectOrDefault(string[] defaultFields)
        {
            return Select ?? defaultFields;
        }
        
        /// <summary>
        /// Expand alanlarının var olup olmadığını kontrol eder
        /// </summary>
        public bool HasExpand()
        {
            return Expand?.Any() == true;
        }
        
        /// <summary>
        /// User için basic select seçeneklerini ayarlar
        /// </summary>
        public static GraphQueryOptions UserBasic()
        {
            return new GraphQueryOptions(GraphSelectFields.User.Basic);
        }
        
        /// <summary>
        /// User için extended select seçeneklerini ayarlar
        /// </summary>
        public static GraphQueryOptions UserExtended()
        {
            return new GraphQueryOptions(GraphSelectFields.User.Extended);
        }
        
        /// <summary>
        /// User için iletişim bilgileri select seçeneklerini ayarlar
        /// </summary>
        public static GraphQueryOptions UserContact()
        {
            return new GraphQueryOptions(GraphSelectFields.User.Contact);
        }
        
        /// <summary>
        /// User için tüm alanları select seçeneklerini ayarlar
        /// </summary>
        public static GraphQueryOptions UserAll()
        {
            return new GraphQueryOptions(GraphSelectFields.User.All);
        }
        
        /// <summary>
        /// User için güvenli tüm alanları select seçeneklerini ayarlar (permission sorunlarını önler)
        /// </summary>
        public static GraphQueryOptions UserAllSafe()
        {
            return new GraphQueryOptions(GraphSelectFields.User.AllSafe);
        }
        
        /// <summary>
        /// Group için basic select seçeneklerini ayarlar
        /// </summary>
        public static GraphQueryOptions GroupBasic()
        {
            return new GraphQueryOptions(GraphSelectFields.Group.Basic);
        }
        
        /// <summary>
        /// Group için extended select seçeneklerini ayarlar
        /// </summary>
        public static GraphQueryOptions GroupExtended()
        {
            return new GraphQueryOptions(GraphSelectFields.Group.Extended);
        }
        
        /// <summary>
        /// Group için security alanları select seçeneklerini ayarlar
        /// </summary>
        public static GraphQueryOptions GroupSecurity()
        {
            return new GraphQueryOptions(GraphSelectFields.Group.Security);
        }
        
        /// <summary>
        /// Group için Teams alanları select seçeneklerini ayarlar
        /// </summary>
        public static GraphQueryOptions GroupTeams()
        {
            return new GraphQueryOptions(GraphSelectFields.Group.Teams);
        }
        
        /// <summary>
        /// Group için tüm alanları select seçeneklerini ayarlar
        /// </summary>
        public static GraphQueryOptions GroupAll()
        {
            return new GraphQueryOptions(GraphSelectFields.Group.All);
        }
        
        /// <summary>
        /// Group için güvenli tüm alanları select seçeneklerini ayarlar (permission sorunlarını önler)
        /// </summary>
        public static GraphQueryOptions GroupAllSafe()
        {
            return new GraphQueryOptions(GraphSelectFields.Group.AllSafe);
        }
        
        /// <summary>
        /// Fluent interface ile select alanlarını ayarlar
        /// </summary>
        public GraphQueryOptions WithSelect(params string[] fields)
        {
            Select = fields;
            return this;
        }
        
        /// <summary>
        /// Fluent interface ile expand alanlarını ayarlar
        /// </summary>
        public GraphQueryOptions WithExpand(params string[] fields)
        {
            Expand = fields;
            return this;
        }
        
        /// <summary>
        /// Mevcut select alanlarına yeni alanlar ekler
        /// </summary>
        public GraphQueryOptions AddSelect(params string[] fields)
        {
            if (Select == null)
            {
                Select = fields;
            }
            else
            {
                Select = Select.Concat(fields).Distinct().ToArray();
            }
            return this;
        }
        
        /// <summary>
        /// Mevcut expand alanlarına yeni alanlar ekler
        /// </summary>
        public GraphQueryOptions AddExpand(params string[] fields)
        {
            if (Expand == null)
            {
                Expand = fields;
            }
            else
            {
                Expand = Expand.Concat(fields).Distinct().ToArray();
            }
            return this;
        }
        
        /// <summary>
        /// Bu seçenekleri kopyalayarak yeni bir instance oluşturur
        /// </summary>
        public GraphQueryOptions Clone()
        {
            return new GraphQueryOptions
            {
                Select = Select?.ToArray(),
                Expand = Expand?.ToArray()
            };
        }
    }
    
    /// <summary>
    /// GraphQueryOptions için extension methods
    /// </summary>
    public static class GraphQueryOptionsExtensions
    {
        /// <summary>
        /// User için manager bilgisi expand eder
        /// </summary>
        public static GraphQueryOptions WithUserManager(this GraphQueryOptions options)
        {
            return options.Clone().AddExpand(GraphExpandFields.User.ManagerWithDetails);
        }
        
        /// <summary>
        /// User için groups bilgisi expand eder
        /// </summary>
        public static GraphQueryOptions WithUserGroups(this GraphQueryOptions options)
        {
            return options.Clone().AddExpand(GraphExpandFields.User.MemberOfWithBasic);
        }
        
        /// <summary>
        /// User için direct reports bilgisi expand eder
        /// </summary>
        public static GraphQueryOptions WithUserDirectReports(this GraphQueryOptions options)
        {
            return options.Clone().AddExpand(GraphExpandFields.User.DirectReportsWithBasic);
        }
        
        /// <summary>
        /// Group için members bilgisi expand eder
        /// </summary>
        public static GraphQueryOptions WithGroupMembers(this GraphQueryOptions options)
        {
            return options.Clone().AddExpand(GraphExpandFields.Group.MembersWithBasic);
        }
        
        /// <summary>
        /// Group için owners bilgisi expand eder
        /// </summary>
        public static GraphQueryOptions WithGroupOwners(this GraphQueryOptions options)
        {
            return options.Clone().AddExpand(GraphExpandFields.Group.OwnersWithBasic);
        }
        
        /// <summary>
        /// Group için parent groups bilgisi expand eder
        /// </summary>
        public static GraphQueryOptions WithGroupParents(this GraphQueryOptions options)
        {
            return options.Clone().AddExpand(GraphExpandFields.Group.MemberOfWithBasic);
        }
    }
}