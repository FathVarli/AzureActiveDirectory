using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureExternalDirectory.Application.GroupService;
using AzureExternalDirectory.Infrastructure.GraphService.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AzureExternalDirectory.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class GroupGraphController : ControllerBase
    {
        private readonly IGroupGraphService _graphGroupService;
        private readonly ILogger<GroupGraphController> _logger;

        public GroupGraphController(
            IGroupGraphService graphGroupService,
            ILogger<GroupGraphController> logger)
        {
            _graphGroupService = graphGroupService;
            _logger = logger;
        }

        /// <summary>
        /// Tüm grupları getirir
        /// </summary>
        /// <param name="top">Alınacak maksimum kayıt sayısı</param>
        /// <param name="filter">OData Filter (örn: "startswith(displayName,'Marketing')")</param>
        /// <param name="search">Arama terimi (displayName içinde arar)</param>
        /// <param name="orderBy">Sıralama kriteri (örn: "displayName", "displayName desc")</param>
        /// <returns>Grup listesi</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllGroups(
            [FromQuery] int? top = null,
            [FromQuery] string? filter = null,
            [FromQuery] string? search = null,
            [FromQuery] string? orderBy = null)
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

                var groups = await _graphGroupService.GetAllGroupsAsync(filterOptions);
                
                return Ok(new { 
                    Count = groups.Count,
                    Data = groups 
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
        /// <returns>Grup bilgileri</returns>
        [HttpGet("{groupId}")]
        public async Task<IActionResult> GetGroupById(string groupId)
        {
            try
            {
                if (string.IsNullOrEmpty(groupId))
                {
                    return BadRequest(new { Message = "Grup ID'si gerekli" });
                }

                var group = await _graphGroupService.GetGroupByIdAsync(groupId);
                
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
        /// <param name="request">Grup ID'leri listesi</param>
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

                var groups = await _graphGroupService.GetGroupsByIdsAsync(request.GroupIds);
                
                return Ok(new { 
                    RequestedCount = request.GroupIds.Count,
                    FoundCount = groups.Count,
                    Data = groups 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Birden fazla grup getirilemedi");
                return StatusCode(500, new { Message = "Grup listesi alınamadı", Error = ex.Message });
            }
        }

        /// <summary>
        /// Bir grubun üyelerini getirir
        /// </summary>
        /// <param name="groupId">Grup ID'si</param>
        /// <returns>Grup üyeleri</returns>
        [HttpGet("{groupId}/members")]
        public async Task<IActionResult> GetGroupMembers(string groupId)
        {
            try
            {
                if (string.IsNullOrEmpty(groupId))
                {
                    return BadRequest(new { Message = "Grup ID'si gerekli" });
                }

                var members = await _graphGroupService.GetGroupMembersAsync(groupId);
                
                return Ok(new { 
                    GroupId = groupId,
                    Count = members.Count,
                    Data = members 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grup üyeleri getirilemedi. GroupId: {GroupId}", groupId);
                return StatusCode(500, new { Message = $"Grup {groupId} üyeleri alınamadı", Error = ex.Message });
            }
        }

        /// <summary>
        /// Birden fazla grubun üyelerini getirir
        /// </summary>
        /// <param name="request">Grup ID'leri listesi</param>
        /// <returns>Grup üyeleri map'i</returns>
        [HttpPost("members")]
        public async Task<IActionResult> GetMultipleGroupMembers([FromBody] GetMultipleGroupMembersRequest request)
        {
            try
            {
                if (request == null || request.GroupIds == null || !request.GroupIds.Any())
                {
                    return BadRequest(new { Message = "Grup ID'leri gerekli" });
                }

                var groupMembers = await _graphGroupService.GetMultipleGroupMembersAsync(request.GroupIds);
                
                return Ok(new { 
                    RequestedGroups = request.GroupIds.Count,
                    Data = groupMembers 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Birden fazla grup üyesi getirilemedi");
                return StatusCode(500, new { Message = "Grup üye listeleri alınamadı", Error = ex.Message });
            }
        }

        /// <summary>
        /// Bir grubun sahiplerini getirir
        /// </summary>
        /// <param name="groupId">Grup ID'si</param>
        /// <returns>Grup sahipleri</returns>
        [HttpGet("{groupId}/owners")]
        public async Task<IActionResult> GetGroupOwners(string groupId)
        {
            try
            {
                if (string.IsNullOrEmpty(groupId))
                {
                    return BadRequest(new { Message = "Grup ID'si gerekli" });
                }

                var owners = await _graphGroupService.GetGroupOwnersAsync(groupId);
                
                return Ok(new { 
                    GroupId = groupId,
                    Count = owners.Count,
                    Data = owners 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grup sahipleri getirilemedi. GroupId: {GroupId}", groupId);
                return StatusCode(500, new { Message = $"Grup {groupId} sahipleri alınamadı", Error = ex.Message });
            }
        }

        /// <summary>
        /// Grup arama
        /// </summary>
        /// <param name="q">Arama terimi</param>
        /// <param name="top">Maksimum sonuç sayısı</param>
        /// <returns>Arama sonuçları</returns>
        [HttpGet("search")]
        public async Task<IActionResult> SearchGroups([FromQuery] string q, [FromQuery] int? top = null)
        {
            try
            {
                if (string.IsNullOrEmpty(q))
                {
                    return BadRequest(new { Message = "Arama terimi gerekli" });
                }

                var groups = await _graphGroupService.SearchGroupsAsync(q, top);
                
                return Ok(new { 
                    SearchTerm = q,
                    Count = groups.Count,
                    Data = groups 
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grup araması başarısız. SearchTerm: {SearchTerm}", q);
                return StatusCode(500, new { Message = "Grup araması yapılamadı", Error = ex.Message });
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

                var count = await _graphGroupService.GetGroupMemberCountAsync(groupId);
                
                return Ok(new { 
                    GroupId = groupId,
                    MemberCount = count 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grup üye sayısı getirilemedi. GroupId: {GroupId}", groupId);
                return StatusCode(500, new { Message = $"Grup {groupId} üye sayısı alınamadı", Error = ex.Message });
            }
        }

        /// <summary>
        /// Kullanıcının grup üyesi olup olmadığını kontrol eder
        /// </summary>
        /// <param name="groupId">Grup ID'si</param>
        /// <param name="userId">Kullanıcı ID'si</param>
        /// <returns>Üyelik durumu</returns>
        [HttpGet("{groupId}/is-member/{userId}")]
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

                var isMember = await _graphGroupService.IsUserGroupMemberAsync(groupId, userId);
                
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
        /// Bir grubun alt gruplarını getirir
        /// </summary>
        /// <param name="groupId">Grup ID'si</param>
        /// <returns>Alt gruplar</returns>
        [HttpGet("{groupId}/sub-groups")]
        public async Task<IActionResult> GetSubGroups(string groupId)
        {
            try
            {
                if (string.IsNullOrEmpty(groupId))
                {
                    return BadRequest(new { Message = "Grup ID'si gerekli" });
                }

                var subGroups = await _graphGroupService.GetSubGroupsAsync(groupId);
                
                return Ok(new { 
                    GroupId = groupId,
                    Count = subGroups.Count,
                    Data = subGroups 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Alt gruplar getirilemedi. GroupId: {GroupId}", groupId);
                return StatusCode(500, new { Message = $"Grup {groupId} alt grupları alınamadı", Error = ex.Message });
            }
        }

        /// <summary>
        /// Bir grubun üye olduğu grupları getirir
        /// </summary>
        /// <param name="groupId">Grup ID'si</param>
        /// <returns>Üst gruplar</returns>
        [HttpGet("{groupId}/parent-groups")]
        public async Task<IActionResult> GetParentGroups(string groupId)
        {
            try
            {
                if (string.IsNullOrEmpty(groupId))
                {
                    return BadRequest(new { Message = "Grup ID'si gerekli" });
                }

                var parentGroups = await _graphGroupService.GetParentGroupsAsync(groupId);
                
                return Ok(new { 
                    GroupId = groupId,
                    Count = parentGroups.Count,
                    Data = parentGroups 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Üst gruplar getirilemedi. GroupId: {GroupId}", groupId);
                return StatusCode(500, new { Message = $"Grup {groupId} üst grupları alınamadı", Error = ex.Message });
            }
        }
    }

    // Request models
    public class GetGroupsByIdsRequest
    {
        public List<string> GroupIds { get; set; } = new List<string>();
    }

    public class GetMultipleGroupMembersRequest
    {
        public List<string> GroupIds { get; set; } = new List<string>();
    }
}