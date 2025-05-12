using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureExternalDirectory.Application.UserService;
using AzureExternalDirectory.Infrastructure.GraphService.Model;
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
        /// <returns>Kullanıcı listesi</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] int? top = null,
            [FromQuery] string? filter = null,
            [FromQuery] string? search = null,
            [FromQuery] string? orderBy = null)
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

                var users = await _graphUserService.GetAllUsersAsync(filterOptions);
                
                return Ok(new { 
                    Count = users.Count,
                    Data = users 
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
        /// <returns>Kullanıcı bilgileri</returns>
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserById(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { Message = "Kullanıcı ID'si gerekli" });
                }

                var user = await _graphUserService.GetUserByIdAsync(userId);
                
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
        /// <param name="request">Kullanıcı ID'leri listesi</param>
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

                var users = await _graphUserService.GetUsersByIdsAsync(request.UserIds);
                
                return Ok(new { 
                    RequestedCount = request.UserIds.Count,
                    FoundCount = users.Count,
                    Data = users 
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
        /// <returns>Kullanıcılar ve grupları</returns>
        [HttpGet("with-groups")]
        public async Task<IActionResult> GetUsersWithGroups(
            [FromQuery] int? top = null,
            [FromQuery] string? filter = null,
            [FromQuery] string? search = null,
            [FromQuery] string? orderBy = null)
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

                var usersWithGroups = await _graphUserService.GetUsersWithGroupsAsync(filterOptions);
                
                return Ok(new { 
                    Count = usersWithGroups.Count,
                    Data = usersWithGroups 
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
        /// <returns>Kullanıcının grupları</returns>
        [HttpGet("{userId}/groups")]
        public async Task<IActionResult> GetUserGroups(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { Message = "Kullanıcı ID'si gerekli" });
                }

                var groups = await _graphUserService.GetUserGroupsAsync(userId);
                
                return Ok(new { 
                    UserId = userId,
                    Count = groups.Count,
                    Data = groups 
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
        /// <returns>Arama sonuçları</returns>
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string q, [FromQuery] int? top = null)
        {
            try
            {
                if (string.IsNullOrEmpty(q))
                {
                    return BadRequest(new { Message = "Arama terimi gerekli" });
                }

                var users = await _graphUserService.SearchUsersAsync(q, top);
                
                return Ok(new { 
                    SearchTerm = q,
                    Count = users.Count,
                    Data = users 
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
    }
    
    // Request models
    public class GetUsersByIdsRequest
    {
        public List<string> UserIds { get; set; } = new List<string>();
    }
}