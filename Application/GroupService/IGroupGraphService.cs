using System.Collections.Generic;
using System.Threading.Tasks;
using AzureExternalDirectory.Infrastructure.GraphService.Model;

namespace AzureExternalDirectory.Application.GroupService
{
   public interface IGroupGraphService
    {
        /// <summary>
        /// Sistemdeki bütün grupları veya belirli bir filtreye göre çeker
        /// </summary>
        /// <param name="filter">Filtreleme seçenekleri</param>
        /// <param name="queryOptions">Select ve Expand seçenekleri</param>
        /// <returns>Grup listesi</returns>
        Task<List<GroupDto>> GetAllGroupsAsync(GroupFilterOptions filter = null, GraphQueryOptions queryOptions = null);

        /// <summary>
        /// ID ile grup getirir
        /// </summary>
        /// <param name="groupId">Grup ID'si</param>
        /// <param name="queryOptions">Select ve Expand seçenekleri</param>
        /// <returns>Grup bilgisi</returns>
        Task<GroupDto> GetGroupByIdAsync(string groupId, GraphQueryOptions queryOptions = null);

        /// <summary>
        /// Birden fazla ID ile grupları getirir
        /// </summary>
        /// <param name="groupIds">Grup ID'leri</param>
        /// <param name="queryOptions">Select ve Expand seçenekleri</param>
        /// <returns>Grup listesi</returns>
        Task<List<GroupDto>> GetGroupsByIdsAsync(List<string> groupIds, GraphQueryOptions queryOptions = null);

        /// <summary>
        /// Grup arama işlemi
        /// </summary>
        /// <param name="searchTerm">Arama terimi</param>
        /// <param name="top">Maksimum sonuç sayısı</param>
        /// <param name="queryOptions">Select ve Expand seçenekleri</param>
        /// <returns>Arama sonuçları</returns>
        Task<List<GroupDto>> SearchGroupsAsync(string searchTerm, int? top = null, GraphQueryOptions queryOptions = null);

        /// <summary>
        /// Bir grubun üye kullanıcılarını getirir
        /// </summary>
        /// <param name="groupId">Grup ID'si</param>
        /// <param name="queryOptions">Select ve Expand seçenekleri</param>
        /// <returns>Kullanıcı listesi</returns>
        Task<List<UserDto>> GetGroupMembersAsync(string groupId, GraphQueryOptions queryOptions = null);

        /// <summary>
        /// Birden fazla grup için üye kullanıcılarını getirir
        /// </summary>
        /// <param name="groupIds">Grup ID'leri</param>
        /// <param name="queryOptions">Select ve Expand seçenekleri</param>
        /// <returns>Grup ID'si ve üyeleri dictionary</returns>
        Task<Dictionary<string, List<UserDto>>> GetMultipleGroupMembersAsync(List<string> groupIds, GraphQueryOptions queryOptions = null);

        /// <summary>
        /// Grup sahiplerini getirir
        /// </summary>
        /// <param name="groupId">Grup ID'si</param>
        /// <param name="queryOptions">Select ve Expand seçenekleri</param>
        /// <returns>Sahip listesi</returns>
        Task<List<UserDto>> GetGroupOwnersAsync(string groupId, GraphQueryOptions queryOptions = null);

        /// <summary>
        /// Grup üye sayısını getirir
        /// </summary>
        /// <param name="groupId">Grup ID'si</param>
        /// <returns>Üye sayısı</returns>
        Task<int> GetGroupMemberCountAsync(string groupId);

        /// <summary>
        /// Kullanıcının grup üyesi olup olmadığını kontrol eder
        /// </summary>
        /// <param name="groupId">Grup ID'si</param>
        /// <param name="userId">Kullanıcı ID'si</param>
        /// <returns>Üyelik durumu</returns>
        Task<bool> IsUserGroupMemberAsync(string groupId, string userId);

        /// <summary>
        /// Alt grupları getirir
        /// </summary>
        /// <param name="groupId">Grup ID'si</param>
        /// <param name="queryOptions">Select ve Expand seçenekleri</param>
        /// <returns>Alt grup listesi</returns>
        Task<List<GroupDto>> GetSubGroupsAsync(string groupId, GraphQueryOptions queryOptions = null);

        /// <summary>
        /// Üst grupları getirir
        /// </summary>
        /// <param name="groupId">Grup ID'si</param>
        /// <param name="queryOptions">Select ve Expand seçenekleri</param>
        /// <returns>Üst grup listesi</returns>
        Task<List<GroupDto>> GetParentGroupsAsync(string groupId, GraphQueryOptions queryOptions = null);
    }
}