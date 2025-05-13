using System.Collections.Generic;
using System.Threading.Tasks;
using AzureExternalDirectory.Infrastructure.GraphService.Model;

namespace AzureExternalDirectory.Application.UserService
{
    public interface IUserGraphService
    {
        /// <summary>
        /// Sistemdeki bütün userları veya belirli bir filtreye göre çeker
        /// </summary>
        /// <param name="filter">Filtreleme seçenekleri</param>
        /// <param name="queryOptions">Select ve Expand seçenekleri</param>
        /// <returns>Kullanıcı listesi</returns>
        Task<List<UserDto>> GetAllUsersAsync(UserFilterOptions filter = null, GraphQueryOptions queryOptions = null);

        /// <summary>
        /// Sistemdeki userları çekerken üye olduğu grupları da getirir
        /// </summary>
        /// <param name="filter">Filtreleme seçenekleri</param>
        /// <param name="userQueryOptions">Kullanıcı alanları için select/expand seçenekleri</param>
        /// <param name="groupQueryOptions">Grup alanları için select/expand seçenekleri</param>
        /// <returns>Kullanıcılar ve grupları</returns>
        Task<List<UserWithGroupsDto>> GetUsersWithGroupsAsync(UserFilterOptions filter = null, GraphQueryOptions userQueryOptions = null, GraphQueryOptions groupQueryOptions = null);

        /// <summary>
        /// ID ile kullanıcı getirir
        /// </summary>
        /// <param name="userId">Kullanıcı ID'si</param>
        /// <param name="queryOptions">Select ve Expand seçenekleri</param>
        /// <returns>Kullanıcı bilgisi</returns>
        Task<UserDto> GetUserByIdAsync(string userId, GraphQueryOptions queryOptions = null);

        /// <summary>
        /// Birden fazla ID ile kullanıcıları getirir
        /// </summary>
        /// <param name="userIds">Kullanıcı ID'leri</param>
        /// <param name="queryOptions">Select ve Expand seçenekleri</param>
        /// <returns>Kullanıcı listesi</returns>
        Task<List<UserDto>> GetUsersByIdsAsync(List<string> userIds, GraphQueryOptions queryOptions = null);

        /// <summary>
        /// Kullanıcı arama işlemi
        /// </summary>
        /// <param name="searchTerm">Arama terimi</param>
        /// <param name="top">Maksimum sonuç sayısı</param>
        /// <param name="queryOptions">Select ve Expand seçenekleri</param>
        /// <returns>Arama sonuçları</returns>
        Task<List<UserDto>> SearchUsersAsync(string searchTerm, int? top = null, GraphQueryOptions queryOptions = null);

        /// <summary>
        /// Kullanıcının üye olduğu grupları getirir
        /// </summary>
        /// <param name="userId">Kullanıcı ID'si</param>
        /// <param name="queryOptions">Select ve Expand seçenekleri</param>
        /// <returns>Grup listesi</returns>
        Task<List<GroupDto>> GetUserGroupsAsync(string userId, GraphQueryOptions queryOptions = null);
    }
}