using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureExternalDirectory.Application.UserService;
using AzureExternalDirectory.Infrastructure.GraphService.Model;
using AzureExternalDirectory.Infrastructure.GraphService.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AzureExternalDirectory.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class UserGraphController : ControllerBase
    {
        private readonly IUserGraphService _graphUserService;
        private readonly ILogger<UserGraphController> _logger;

        public UserGraphController(
            IUserGraphService graphUserService,
            ILogger<UserGraphController> logger)
        {
            _graphUserService = graphUserService;
            _logger = logger;
        }

        /// <summary>
        /// Tüm kullanıcıları getirir
        /// </summary>
        /// <param name="top">Alınacak maksimum kayıt sayısı</param>
        /// <param name="filter">OData Filter (örn: "startswith(displayName,'John')")</param>
        /// <param name="search">Arama terimi (displayName, mail içinde arar)</param>
        /// <param name="orderBy">Sıralama kriteri (örn: "displayName", "displayName desc")</param>
        /// <param name="select">Virgül ile ayrılmış select alanları (örn: "id,displayName,mail")</param>
        /// <param name="expand">Virgül ile ayrılmış expand alanları (örn: "manager,memberOf")</param>
        /// <param name="fields">Önceden tanımlı alan seti (basic, default, extended, contact, all)</param>
        /// <returns>Kullanıcı listesi</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] int? top = null,
            [FromQuery] string filter = null,
            [FromQuery] string search = null,
            [FromQuery] string orderBy = null,
            [FromQuery] string select = null,
            [FromQuery] string expand = null,
            [FromQuery] string fields = null)
        {
            try
            {
                var filterOptions = new UserFilterOptions
                {
                    Top = top,
                    Filter = filter,
                    Search = search,
                    OrderBy = orderBy
                };

                var queryOptions = ParseQueryOptions(select, expand, fields);

                var users = await _graphUserService.GetAllUsersAsync(filterOptions, queryOptions);
                
                return Ok(new { 
                    Count = users.Count,
                    Data = users,
                    QueryInfo = new
                    {
                        Select = queryOptions.Select,
                        Expand = queryOptions.Expand
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcılar getirilemedi");
                return StatusCode(500, new { Message = "Kullanıcı listesi alınamadı", Error = ex.Message });
            }
        }

        /// <summary>
        /// ID ile kullanıcı getirir
        /// </summary>
        /// <param name="userId">Kullanıcı ID'si</param>
        /// <param name="select">Virgül ile ayrılmış select alanları</param>
        /// <param name="expand">Virgül ile ayrılmış expand alanları</param>
        /// <param name="fields">Önceden tanımlı alan seti (basic, default, extended, contact, all)</param>
        /// <returns>Kullanıcı bilgileri</returns>
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserById(
            string userId,
            [FromQuery] string select = null,
            [FromQuery] string expand = null,
            [FromQuery] string fields = null)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { Message = "Kullanıcı ID'si gerekli" });
                }

                var queryOptions = ParseQueryOptions(select, expand, fields);

                var user = await _graphUserService.GetUserByIdAsync(userId, queryOptions);
                
                if (user == null)
                {
                    return NotFound(new { Message = $"Kullanıcı {userId} bulunamadı" });
                }
                
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı getirilemedi. UserId: {UserId}", userId);
                return StatusCode(500, new { Message = "Kullanıcı alınamadı", Error = ex.Message });
            }
        }

        /// <summary>
        /// Birden fazla kullanıcıyı ID'leri ile getirir
        /// </summary>
        /// <param name="request">Kullanıcı ID'leri ve sorgu seçenekleri</param>
        /// <returns>Bulunan kullanıcılar</returns>
        [HttpPost("by-ids")]
        public async Task<IActionResult> GetUsersByIds([FromBody] GetUsersByIdsRequest request)
        {
            try
            {
                if (request == null || request.UserIds == null || !request.UserIds.Any())
                {
                    return BadRequest(new { Message = "Kullanıcı ID'leri gerekli" });
                }

                var queryOptions = ParseQueryOptions(request.Select, request.Expand, request.Fields);

                var users = await _graphUserService.GetUsersByIdsAsync(request.UserIds, queryOptions);
                
                return Ok(new { 
                    RequestedCount = request.UserIds.Count,
                    FoundCount = users.Count,
                    Data = users,
                    QueryInfo = new
                    {
                        Select = queryOptions.Select,
                        Expand = queryOptions.Expand
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Birden fazla kullanıcı getirilemedi");
                return StatusCode(500, new { Message = "Kullanıcı listesi alınamadı", Error = ex.Message });
            }
        }

        /// <summary>
        /// Kullanıcıları gruplarıyla birlikte getirir
        /// </summary>
        /// <param name="top">Alınacak maksimum kullanıcı sayısı</param>
        /// <param name="filter">OData Filter</param>
        /// <param name="search">Arama terimi</param>
        /// <param name="orderBy">Sıralama kriteri</param>
        /// <param name="userSelect">Kullanıcı alanları için select</param>
        /// <param name="userExpand">Kullanıcı alanları için expand</param>
        /// <param name="userFields">Kullanıcı için önceden tanımlı alan seti</param>
        /// <param name="groupSelect">Grup alanları için select</param>
        /// <param name="groupExpand">Grup alanları için expand</param>
        /// <param name="groupFields">Grup için önceden tanımlı alan seti</param>
        /// <returns>Kullanıcılar ve grupları</returns>
        [HttpGet("with-groups")]
        public async Task<IActionResult> GetUsersWithGroups(
            [FromQuery] int? top = null,
            [FromQuery] string filter = null,
            [FromQuery] string search = null,
            [FromQuery] string orderBy = null,
            [FromQuery] string userSelect = null,
            [FromQuery] string userExpand = null,
            [FromQuery] string userFields = null,
            [FromQuery] string groupSelect = null,
            [FromQuery] string groupExpand = null,
            [FromQuery] string groupFields = null)
        {
            try
            {
                var filterOptions = new UserFilterOptions
                {
                    Top = top,
                    Filter = filter,
                    Search = search,
                    OrderBy = orderBy
                };

                var userQueryOptions = ParseQueryOptions(userSelect, userExpand, userFields);
                var groupQueryOptions = ParseGroupQueryOptions(groupSelect, groupExpand, groupFields);

                var usersWithGroups = await _graphUserService.GetUsersWithGroupsAsync(filterOptions, userQueryOptions, groupQueryOptions);
                
                return Ok(new { 
                    Count = usersWithGroups.Count,
                    Data = usersWithGroups,
                    QueryInfo = new
                    {
                        UserQuery = new { Select = userQueryOptions.Select, Expand = userQueryOptions.Expand },
                        GroupQuery = new { Select = groupQueryOptions.Select, Expand = groupQueryOptions.Expand }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcılar gruplarıyla getirilemedi");
                return StatusCode(500, new { Message = "Kullanıcı ve grup listesi alınamadı", Error = ex.Message });
            }
        }

        /// <summary>
        /// Kullanıcının üye olduğu grupları getirir
        /// </summary>
        /// <param name="userId">Kullanıcı ID'si</param>
        /// <param name="select">Virgül ile ayrılmış select alanları</param>
        /// <param name="expand">Virgül ile ayrılmış expand alanları</param>
        /// <param name="fields">Önceden tanımlı alan seti (basic, default, extended, security, teams, all)</param>
        /// <returns>Kullanıcının grupları</returns>
        [HttpGet("{userId}/groups")]
        public async Task<IActionResult> GetUserGroups(
            string userId,
            [FromQuery] string select = null,
            [FromQuery] string expand = null,
            [FromQuery] string fields = null)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { Message = "Kullanıcı ID'si gerekli" });
                }

                var queryOptions = ParseGroupQueryOptions(select, expand, fields);

                var groups = await _graphUserService.GetUserGroupsAsync(userId, queryOptions);
                
                return Ok(new { 
                    UserId = userId,
                    Count = groups.Count,
                    Data = groups,
                    QueryInfo = new
                    {
                        Select = queryOptions.Select,
                        Expand = queryOptions.Expand
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı grupları getirilemedi. UserId: {UserId}", userId);
                return StatusCode(500, new { Message = $"Kullanıcı {userId} grupları alınamadı", Error = ex.Message });
            }
        }

        /// <summary>
        /// Kullanıcı arama
        /// </summary>
        /// <param name="q">Arama terimi</param>
        /// <param name="top">Maksimum sonuç sayısı</param>
        /// <param name="select">Virgül ile ayrılmış select alanları</param>
        /// <param name="expand">Virgül ile ayrılmış expand alanları</param>
        /// <param name="fields">Önceden tanımlı alan seti</param>
        /// <returns>Arama sonuçları</returns>
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers(
            [FromQuery] string q,
            [FromQuery] int? top = null,
            [FromQuery] string select = null,
            [FromQuery] string expand = null,
            [FromQuery] string fields = null)
        {
            try
            {
                if (string.IsNullOrEmpty(q))
                {
                    return BadRequest(new { Message = "Arama terimi gerekli" });
                }

                var queryOptions = ParseQueryOptions(select, expand, fields);

                var users = await _graphUserService.SearchUsersAsync(q, top, queryOptions);
                
                return Ok(new { 
                    SearchTerm = q,
                    Count = users.Count,
                    Data = users,
                    QueryInfo = new
                    {
                        Select = queryOptions.Select,
                        Expand = queryOptions.Expand
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı araması başarısız. SearchTerm: {SearchTerm}", q);
                return StatusCode(500, new { Message = "Kullanıcı araması yapılamadı", Error = ex.Message });
            }
        }

        /// <summary>
        /// Kullanılabilir select alanlarını listeler
        /// </summary>
        /// <returns>Select alanları</returns>
        [HttpGet("fields")]
        public IActionResult GetAvailableFields()
        {
            return Ok(new
            {
                UserFields = new
                {
                    PredefinedSets = new
                    {
                        basic = GraphSelectFields.User.Basic,
                        @default = GraphSelectFields.User.Default,
                        extended = GraphSelectFields.User.Extended,
                        contact = GraphSelectFields.User.Contact,
                        all = GraphSelectFields.User.AllSafe // AllSafe gösterSafe // AllSafe kullan
                    },
                    AllFields = typeof(GraphSelectFields.User).GetFields()
                        .Where(f => f.IsLiteral && f.IsStatic && f.FieldType == typeof(string))
                        .Select(f => new { 
                            Name = f.Name, 
                            Value = f.GetRawConstantValue()?.ToString() 
                        })
                        .ToList(),
                    ExpandOptions = new
                    {
                        manager = GraphExpandFields.User.ManagerWithDetails,
                        groups = GraphExpandFields.User.MemberOfWithBasic,
                        directReports = GraphExpandFields.User.DirectReportsWithBasic
                    }
                },
                GroupFields = new
                {
                    PredefinedSets = new
                    {
                        basic = GraphSelectFields.Group.Basic,
                        @default = GraphSelectFields.Group.Default,
                        extended = GraphSelectFields.Group.Extended,
                        security = GraphSelectFields.Group.Security,
                        teams = GraphSelectFields.Group.Teams,
                        all = GraphSelectFields.Group.All
                    }
                }
            });
        }

        // Helper metodları
        private GraphQueryOptions ParseQueryOptions(string select, string expand, string fields)
        {
            var queryOptions = new GraphQueryOptions();

            // Önce fields parametresinden set belirle
            if (!string.IsNullOrEmpty(fields))
            {
                switch (fields.ToLower())
                {
                    case "basic":
                        queryOptions = GraphQueryOptions.UserBasic();
                        break;
                    case "default":
                        queryOptions.Select = GraphSelectFields.User.Default;
                        break;
                    case "extended":
                        queryOptions = GraphQueryOptions.UserExtended();
                        break;
                    case "contact":
                        queryOptions = GraphQueryOptions.UserContact();
                        break;
                    case "all":
                        queryOptions = GraphQueryOptions.UserAllSafe(); // UserAll() yerine UserAllSafe() kullan
                        break;
                }
            }

            // Sonra özel select/expand varsa override et
            if (!string.IsNullOrEmpty(select))
            {
                queryOptions.Select = select.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToArray();
            }

            if (!string.IsNullOrEmpty(expand))
            {
                queryOptions.Expand = expand.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToArray();
            }

            return queryOptions;
        }

        private GraphQueryOptions ParseGroupQueryOptions(string select, string expand, string fields)
        {
            var queryOptions = new GraphQueryOptions();

            // Önce fields parametresinden set belirle
            if (!string.IsNullOrEmpty(fields))
            {
                switch (fields.ToLower())
                {
                    case "basic":
                        queryOptions = GraphQueryOptions.GroupBasic();
                        break;
                    case "default":
                        queryOptions.Select = GraphSelectFields.Group.Default;
                        break;
                    case "extended":
                        queryOptions = GraphQueryOptions.GroupExtended();
                        break;
                    case "security":
                        queryOptions = GraphQueryOptions.GroupSecurity();
                        break;
                    case "teams":
                        queryOptions = GraphQueryOptions.GroupTeams();
                        break;
                    case "all":
                        queryOptions = GraphQueryOptions.GroupAll();
                        break;
                }
            }

            // Sonra özel select/expand varsa override et
            if (!string.IsNullOrEmpty(select))
            {
                queryOptions.Select = select.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToArray();
            }

            if (!string.IsNullOrEmpty(expand))
            {
                queryOptions.Expand = expand.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToArray();
            }

            return queryOptions;
        }
    }
    
    // Request models
    public class GetUsersByIdsRequest
    {
        public List<string> UserIds { get; set; } = new List<string>();
        public string Select { get; set; }
        public string Expand { get; set; }
        public string Fields { get; set; }
    }
}