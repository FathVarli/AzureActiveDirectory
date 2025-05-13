using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureExternalDirectory.Application.GroupService;
using AzureExternalDirectory.Infrastructure.GraphService.Model;
using AzureExternalDirectory.Infrastructure.GraphService.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AzureExternalDirectory.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class GroupGraphController : ControllerBase
    {
        private readonly IGroupGraphService _groupGraphService;
        private readonly ILogger<GroupGraphController> _logger;

        public GroupGraphController(
            IGroupGraphService groupGraphService,
            ILogger<GroupGraphController> logger)
        {
            _groupGraphService = groupGraphService;
            _logger = logger;
        }

        /// <summary>
        /// Tüm grupları getirir
        /// </summary>
        /// <param name="top">Alınacak maksimum kayıt sayısı</param>
        /// <param name="filter">OData Filter (örn: "startswith(displayName,'Sales')")</param>
        /// <param name="search">Arama terimi (displayName, description içinde arar)</param>
        /// <param name="orderBy">Sıralama kriteri (örn: "displayName", "displayName desc")</param>
        /// <param name="select">Virgül ile ayrılmış select alanları (örn: "id,displayName,mail")</param>
        /// <param name="expand">Virgül ile ayrılmış expand alanları (örn: "members,owners")</param>
        /// <param name="fields">Önceden tanımlı alan seti (basic, default, extended, security, teams, all)</param>
        /// <returns>Grup listesi</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllGroups(
            [FromQuery] int? top = null,
            [FromQuery] string? filter = null,
            [FromQuery] string? search = null,
            [FromQuery] string? orderBy = null,
            [FromQuery] string? select = null,
            [FromQuery] string? expand = null,
            [FromQuery] string? fields = null)
        {
            try
            {
                var filterOptions = new GroupFilterOptions
                {
                    Top = top,
                    Filter = filter,
                    Search = search,
                    OrderBy = orderBy
                };

                var queryOptions = ParseGroupQueryOptions(select, expand, fields);

                var groups = await _groupGraphService.GetAllGroupsAsync(filterOptions, queryOptions);
                
                return Ok(new { 
                    Count = groups.Count,
                    Data = groups,
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
                _logger.LogError(ex, "Gruplar getirilemedi");
                return StatusCode(500, new { Message = "Grup listesi alınamadı", Error = ex.Message });
            }
        }

        /// <summary>
        /// ID ile grup getirir
        /// </summary>
        /// <param name="groupId">Grup ID'si</param>
        /// <param name="select">Virgül ile ayrılmış select alanları</param>
        /// <param name="expand">Virgül ile ayrılmış expand alanları</param>
        /// <param name="fields">Önceden tanımlı alan seti (basic, default, extended, security, teams, all)</param>
        /// <returns>Grup bilgileri</returns>
        [HttpGet("{groupId}")]
        public async Task<IActionResult> GetGroupById(
            string groupId,
            [FromQuery] string? select = null,
            [FromQuery] string? expand = null,
            [FromQuery] string? fields = null)
        {
            try
            {
                if (string.IsNullOrEmpty(groupId))
                {
                    return BadRequest(new { Message = "Grup ID'si gerekli" });
                }

                var queryOptions = ParseGroupQueryOptions(select, expand, fields);

                var group = await _groupGraphService.GetGroupByIdAsync(groupId, queryOptions);
                
                if (group == null)
                {
                    return NotFound(new { Message = $"Grup {groupId} bulunamadı" });
                }
                
                return Ok(group);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grup getirilemedi. GroupId: {GroupId}", groupId);
                return StatusCode(500, new { Message = "Grup alınamadı", Error = ex.Message });
            }
        }

        /// <summary>
        /// Birden fazla grubu ID'leri ile getirir
        /// </summary>
        /// <param name="request">Grup ID'leri ve sorgu seçenekleri</param>
        /// <returns>Bulunan gruplar</returns>
        [HttpPost("by-ids")]
        public async Task<IActionResult> GetGroupsByIds([FromBody] GetGroupsByIdsRequest request)
        {
            try
            {
                if (request == null || request.GroupIds == null || !request.GroupIds.Any())
                {
                    return BadRequest(new { Message = "Grup ID'leri gerekli" });
                }

                var queryOptions = ParseGroupQueryOptions(request.Select, request.Expand, request.Fields);

                var groups = await _groupGraphService.GetGroupsByIdsAsync(request.GroupIds, queryOptions);
                
                return Ok(new { 
                    RequestedCount = request.GroupIds.Count,
                    FoundCount = groups.Count,
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
                _logger.LogError(ex, "Birden fazla grup getirilemedi");
                return StatusCode(500, new { Message = "Grup listesi alınamadı", Error = ex.Message });
            }
        }

        /// <summary>
        /// Grup arama - NOT: Microsoft Graph search formatı gerektirir. Alternatif olarak filter kullanabilirsiniz.
        /// </summary>
        /// <param name="q">Arama terimi (örn: "Software" veya "displayName:Software")</param>
        /// <param name="top">Maksimum sonuç sayısı</param>
        /// <param name="select">Virgül ile ayrılmış select alanları</param>
        /// <param name="expand">Virgül ile ayrılmış expand alanları</param>
        /// <param name="fields">Önceden tanımlı alan seti</param>
        /// <returns>Arama sonuçları</returns>
        [HttpGet("search")]
        public async Task<IActionResult> SearchGroups(
            [FromQuery] string q,
            [FromQuery] int? top = null,
            [FromQuery] string? select = null,
            [FromQuery] string? expand = null,
            [FromQuery] string? fields = null)
        {
            try
            {
                if (string.IsNullOrEmpty(q))
                {
                    return BadRequest(new { 
                        Message = "Arama terimi gerekli",
                        Hint = "Microsoft Graph search formatı: 'displayName:Software' veya alternatif olarak GET /api/groupgraph?filter=startswith(displayName,'Software') kullanın"
                    });
                }

                var queryOptions = ParseGroupQueryOptions(select, expand, fields);

                var groups = await _groupGraphService.SearchGroupsAsync(q, top, queryOptions);
                
                return Ok(new { 
                    SearchTerm = q,
                    Count = groups.Count,
                    Data = groups,
                    QueryInfo = new
                    {
                        Select = queryOptions.Select,
                        Expand = queryOptions.Expand
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { 
                    Message = ex.Message,
                    Hint = "Microsoft Graph search formatı kullanın: 'displayName:Software' veya filter endpoint'ini tercih edin"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grup araması başarısız. SearchTerm: {SearchTerm}", q);
                return StatusCode(500, new { 
                    Message = "Grup araması yapılamadı", 
                    Error = ex.Message,
                    Suggestion = "Alternatif olarak GET /api/groupgraph?filter=startswith(displayName,'Software')&expand=members kullanın"
                });
            }
        }

        /// <summary>
        /// Grup üyelerini getirir
        /// </summary>
        /// <param name="groupId">Grup ID'si</param>
        /// <param name="select">Virgül ile ayrılmış select alanları</param>
        /// <param name="expand">Virgül ile ayrılmış expand alanları</param>
        /// <param name="fields">Önceden tanımlı alan seti (basic, default, extended, contact, all)</param>
        /// <returns>Grup üyeleri</returns>
        [HttpGet("{groupId}/members")]
        public async Task<IActionResult> GetGroupMembers(
            string groupId,
            [FromQuery] string? select = null,
            [FromQuery] string? expand = null,
            [FromQuery] string? fields = null)
        {
            try
            {
                if (string.IsNullOrEmpty(groupId))
                {
                    return BadRequest(new { Message = "Grup ID'si gerekli" });
                }

                var queryOptions = ParseUserQueryOptions(select, expand, fields);

                var members = await _groupGraphService.GetGroupMembersAsync(groupId, queryOptions);
                
                return Ok(new { 
                    GroupId = groupId,
                    Count = members.Count,
                    Data = members,
                    QueryInfo = new
                    {
                        Select = queryOptions.Select,
                        Expand = queryOptions.Expand
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grup üyeleri getirilemedi. GroupId: {GroupId}", groupId);
                return StatusCode(500, new { Message = $"Grup {groupId} üyeleri alınamadı", Error = ex.Message });
            }
        }

        /// <summary>
        /// Grup sahiplerini getirir
        /// </summary>
        /// <param name="groupId">Grup ID'si</param>
        /// <param name="select">Virgül ile ayrılmış select alanları</param>
        /// <param name="expand">Virgül ile ayrılmış expand alanları</param>
        /// <param name="fields">Önceden tanımlı alan seti (basic, default, extended, contact, all)</param>
        /// <returns>Grup sahipleri</returns>
        [HttpGet("{groupId}/owners")]
        public async Task<IActionResult> GetGroupOwners(
            string groupId,
            [FromQuery] string? select = null,
            [FromQuery] string? expand = null,
            [FromQuery] string? fields = null)
        {
            try
            {
                if (string.IsNullOrEmpty(groupId))
                {
                    return BadRequest(new { Message = "Grup ID'si gerekli" });
                }

                var queryOptions = ParseUserQueryOptions(select, expand, fields);

                var owners = await _groupGraphService.GetGroupOwnersAsync(groupId, queryOptions);
                
                return Ok(new { 
                    GroupId = groupId,
                    Count = owners.Count,
                    Data = owners,
                    QueryInfo = new
                    {
                        Select = queryOptions.Select,
                        Expand = queryOptions.Expand
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grup sahipleri getirilemedi. GroupId: {GroupId}", groupId);
                return StatusCode(500, new { Message = $"Grup {groupId} sahipleri alınamadı", Error = ex.Message });
            }
        }

        /// <summary>
        /// Grup üye sayısını getirir
        /// </summary>
        /// <param name="groupId">Grup ID'si</param>
        /// <returns>Üye sayısı</returns>
        [HttpGet("{groupId}/member-count")]
        public async Task<IActionResult> GetGroupMemberCount(string groupId)
        {
            try
            {
                if (string.IsNullOrEmpty(groupId))
                {
                    return BadRequest(new { Message = "Grup ID'si gerekli" });
                }

                var count = await _groupGraphService.GetGroupMemberCountAsync(groupId);
                
                return Ok(new { 
                    GroupId = groupId,
                    MemberCount = count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grup üye sayısı alınamadı. GroupId: {GroupId}", groupId);
                return StatusCode(500, new { Message = $"Grup {groupId} üye sayısı alınamadı", Error = ex.Message });
            }
        }

        /// <summary>
        /// Kullanıcının grup üyesi olup olmadığını kontrol eder
        /// </summary>
        /// <param name="groupId">Grup ID'si</param>
        /// <param name="userId">Kullanıcı ID'si</param>
        /// <returns>Üyelik durumu</returns>
        [HttpGet("{groupId}/members/{userId}/check")]
        public async Task<IActionResult> IsUserGroupMember(string groupId, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(groupId))
                {
                    return BadRequest(new { Message = "Grup ID'si gerekli" });
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { Message = "Kullanıcı ID'si gerekli" });
                }

                var isMember = await _groupGraphService.IsUserGroupMemberAsync(groupId, userId);
                
                return Ok(new { 
                    GroupId = groupId,
                    UserId = userId,
                    IsMember = isMember
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grup üyeliği kontrol edilemedi. GroupId: {GroupId}, UserId: {UserId}", groupId, userId);
                return StatusCode(500, new { Message = "Grup üyeliği kontrol edilemedi", Error = ex.Message });
            }
        }

        /// <summary>
        /// Alt grupları getirir
        /// </summary>
        /// <param name="groupId">Grup ID'si</param>
        /// <param name="select">Virgül ile ayrılmış select alanları</param>
        /// <param name="expand">Virgül ile ayrılmış expand alanları</param>
        /// <param name="fields">Önceden tanımlı alan seti (basic, default, extended, security, teams, all)</param>
        /// <returns>Alt gruplar</returns>
        [HttpGet("{groupId}/sub-groups")]
        public async Task<IActionResult> GetSubGroups(
            string groupId,
            [FromQuery] string? select = null,
            [FromQuery] string? expand = null,
            [FromQuery] string? fields = null)
        {
            try
            {
                if (string.IsNullOrEmpty(groupId))
                {
                    return BadRequest(new { Message = "Grup ID'si gerekli" });
                }

                var queryOptions = ParseGroupQueryOptions(select, expand, fields);

                var subGroups = await _groupGraphService.GetSubGroupsAsync(groupId, queryOptions);
                
                return Ok(new { 
                    GroupId = groupId,
                    Count = subGroups.Count,
                    Data = subGroups,
                    QueryInfo = new
                    {
                        Select = queryOptions.Select,
                        Expand = queryOptions.Expand
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Alt gruplar getirilemedi. GroupId: {GroupId}", groupId);
                return StatusCode(500, new { Message = $"Grup {groupId} alt grupları alınamadı", Error = ex.Message });
            }
        }

        /// <summary>
        /// Üst grupları getirir
        /// </summary>
        /// <param name="groupId">Grup ID'si</param>
        /// <param name="select">Virgül ile ayrılmış select alanları</param>
        /// <param name="expand">Virgül ile ayrılmış expand alanları</param>
        /// <param name="fields">Önceden tanımlı alan seti (basic, default, extended, security, teams, all)</param>
        /// <returns>Üst gruplar</returns>
        [HttpGet("{groupId}/parent-groups")]
        public async Task<IActionResult> GetParentGroups(
            string groupId,
            [FromQuery] string? select = null,
            [FromQuery] string? expand = null,
            [FromQuery] string? fields = null)
        {
            try
            {
                if (string.IsNullOrEmpty(groupId))
                {
                    return BadRequest(new { Message = "Grup ID'si gerekli" });
                }

                var queryOptions = ParseGroupQueryOptions(select, expand, fields);

                var parentGroups = await _groupGraphService.GetParentGroupsAsync(groupId, queryOptions);
                
                return Ok(new { 
                    GroupId = groupId,
                    Count = parentGroups.Count,
                    Data = parentGroups,
                    QueryInfo = new
                    {
                        Select = queryOptions.Select,
                        Expand = queryOptions.Expand
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Üst gruplar getirilemedi. GroupId: {GroupId}", groupId);
                return StatusCode(500, new { Message = $"Grup {groupId} üst grupları alınamadı", Error = ex.Message });
            }
        }

        /// <summary>
        /// Birden fazla grup için üyeleri getirir
        /// </summary>
        /// <param name="request">Grup ID'leri ve sorgu seçenekleri</param>
        /// <returns>Gruplar ve üyeleri</returns>
        [HttpPost("multiple-members")]
        public async Task<IActionResult> GetMultipleGroupMembers([FromBody] GetMultipleGroupMembersRequest request)
        {
            try
            {
                if (request == null || request.GroupIds == null || !request.GroupIds.Any())
                {
                    return BadRequest(new { Message = "Grup ID'leri gerekli" });
                }

                var queryOptions = ParseUserQueryOptions(request.Select, request.Expand, request.Fields);

                var groupMembers = await _groupGraphService.GetMultipleGroupMembersAsync(request.GroupIds, queryOptions);
                
                return Ok(new { 
                    RequestedCount = request.GroupIds.Count,
                    Data = groupMembers,
                    QueryInfo = new
                    {
                        Select = queryOptions.Select,
                        Expand = queryOptions.Expand
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Birden fazla grup üyesi getirilemedi");
                return StatusCode(500, new { Message = "Birden fazla grup üyesi alınamadı", Error = ex.Message });
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
                GroupFields = new
                {
                    PredefinedSets = new
                    {
                        basic = GraphSelectFields.Group.Basic,
                        @default = GraphSelectFields.Group.Default,
                        extended = GraphSelectFields.Group.Extended,
                        security = GraphSelectFields.Group.Security,
                        teams = GraphSelectFields.Group.Teams,
                        all = GraphSelectFields.Group.AllSafe // AllSafe kullan
                    },
                    AllFields = typeof(GraphSelectFields.Group).GetFields()
                        .Where(f => f.IsLiteral && f.IsStatic && f.FieldType == typeof(string))
                        .Select(f => new { 
                            Name = f.Name, 
                            Value = f.GetRawConstantValue()?.ToString() 
                        })
                        .ToList(),
                    ExpandOptions = new
                    {
                        members = GraphExpandFields.Group.MembersWithBasic,
                        owners = GraphExpandFields.Group.OwnersWithBasic,
                        memberOf = GraphExpandFields.Group.MemberOfWithBasic
                    }
                },
                UserFields = new
                {
                    PredefinedSets = new
                    {
                        basic = GraphSelectFields.User.Basic,
                        @default = GraphSelectFields.User.Default,
                        extended = GraphSelectFields.User.Extended,
                        contact = GraphSelectFields.User.Contact,
                        all = GraphSelectFields.User.All
                    }
                }
            });
        }

        // Helper metodları
        private GraphQueryOptions ParseGroupQueryOptions(string? select, string? expand, string? fields)
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
                        queryOptions = GraphQueryOptions.GroupAllSafe(); // GroupAll() yerine GroupAllSafe() kullan
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

        private GraphQueryOptions ParseUserQueryOptions(string? select, string? expand, string? fields)
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
                        queryOptions = GraphQueryOptions.UserAll();
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
    public class GetGroupsByIdsRequest
    {
        public List<string> GroupIds { get; set; } = new List<string>();
        public string? Select { get; set; }
        public string? Expand { get; set; }
        public string? Fields { get; set; }
    }

    public class GetMultipleGroupMembersRequest
    {
        public List<string> GroupIds { get; set; } = new List<string>();
        public string? Select { get; set; }
        public string? Expand { get; set; }
        public string? Fields { get; set; }
    }
}