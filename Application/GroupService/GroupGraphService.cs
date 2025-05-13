using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureExternalDirectory.Domain;
using AzureExternalDirectory.Infrastructure.GraphService.Factory;
using AzureExternalDirectory.Infrastructure.GraphService.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace AzureExternalDirectory.Application.GroupService
{
    public class GraphGroupService : IGroupGraphService
    {
        private readonly IGraphServiceFactory _graphServiceFactory;
        private readonly AzureAdConfiguration _azureAdConfiguration;
        private readonly ILogger<GraphGroupService> _logger;

        public GraphGroupService(
            IGraphServiceFactory graphServiceFactory,
            IOptions<AzureAdConfiguration> azureAdConfiguration,
            ILogger<GraphGroupService> logger)
        {
            _graphServiceFactory = graphServiceFactory;
            _azureAdConfiguration = azureAdConfiguration.Value;
            _logger = logger;
        }

        // 1. Sistemdeki bütün grupları veya belirli bir filtreye göre çeken (parametrik select/expand)
        public async Task<List<GroupDto>> GetAllGroupsAsync(GroupFilterOptions? filter = null, GraphQueryOptions? queryOptions = null)
        {
            GraphServiceClient? client = null;
            try
            {
                // QueryOptions öncelik sırası: parametre > filter.QueryOptions > default
                var resolvedQueryOptions = queryOptions ?? filter?.GetQueryOptionsOrDefault() ?? new GraphQueryOptions();
                
                _logger.LogInformation("Gruplar getiriliyor. Filter: {@Filter}, Select: {@Select}, Expand: {@Expand}", 
                    filter, resolvedQueryOptions.Select, resolvedQueryOptions.Expand);

                client = await _graphServiceFactory.CreateSystemClientAsync(_azureAdConfiguration);

                var allGroups = new List<Group>();
                string? nextPageUrl = null;

                do
                {
                    GroupCollectionResponse? groups;
                    
                    if (nextPageUrl != null)
                    {
                        groups = await client.Groups.WithUrl(nextPageUrl).GetAsync();
                    }
                    else
                    {
                        groups = await client.Groups.GetAsync(config =>
                        {
                            // Select alanları uygula
                            config.QueryParameters.Select = resolvedQueryOptions.GetGroupSelectOrDefault();
                            
                            // Expand alanları varsa uygula
                            if (resolvedQueryOptions.Expand?.Any() == true)
                            {
                                config.QueryParameters.Expand = resolvedQueryOptions.Expand;
                            }
                            
                            if (filter != null)
                            {
                                if (filter.Top.HasValue)
                                    config.QueryParameters.Top = filter.Top.Value;
                                
                                if (!string.IsNullOrEmpty(filter.Filter))
                                    config.QueryParameters.Filter = filter.Filter;
                                
                                if (!string.IsNullOrEmpty(filter.Search))
                                    config.QueryParameters.Search = filter.Search;
                                
                                if (!string.IsNullOrEmpty(filter.OrderBy))
                                    config.QueryParameters.Orderby = new[] { filter.OrderBy };
                            }
                        });
                    }

                    if (groups?.Value != null)
                    {
                        allGroups.AddRange(groups.Value);
                        nextPageUrl = groups.OdataNextLink;
                    }
                    else
                    {
                        break;
                    }

                    if (filter?.Top.HasValue == true && allGroups.Count >= filter.Top.Value)
                    {
                        allGroups = allGroups.Take(filter.Top.Value).ToList();
                        break;
                    }

                } while (nextPageUrl != null);

                var result = allGroups.Select(MapGroupToDto).ToList();
                
                _logger.LogInformation("Toplam {Count} grup getirildi", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gruplar getirilemedi");
                throw new InvalidOperationException("Grup listesi alınamadı", ex);
            }
            finally
            {
                if (client != null)
                {
                    _graphServiceFactory.DisposeClient(client);
                }
            }
        }

        // 2. ID ile grup getir (parametrik select/expand)
        public async Task<GroupDto?> GetGroupByIdAsync(string groupId, GraphQueryOptions? queryOptions = null)
        {
            GraphServiceClient? client = null;
            try
            {
                var resolvedQueryOptions = queryOptions ?? new GraphQueryOptions();
                
                _logger.LogInformation("Grup getiriliyor. GroupId: {GroupId}, Select: {@Select}", 
                    groupId, resolvedQueryOptions.Select);

                client = await _graphServiceFactory.CreateSystemClientAsync(_azureAdConfiguration);

                var group = await client.Groups[groupId].GetAsync(config =>
                {
                    config.QueryParameters.Select = resolvedQueryOptions.GetGroupSelectOrDefault();
                    
                    if (resolvedQueryOptions.Expand?.Any() == true)
                    {
                        config.QueryParameters.Expand = resolvedQueryOptions.Expand;
                    }
                });

                if (group == null)
                {
                    _logger.LogWarning("Grup bulunamadı. GroupId: {GroupId}", groupId);
                    return null;
                }

                var result = MapGroupToDto(group);
                _logger.LogInformation("Grup getirildi: {DisplayName}", result.DisplayName);
                return result;
            }
            catch (Microsoft.Graph.Models.ODataErrors.ODataError ex) when (ex.ResponseStatusCode == 404)
            {
                _logger.LogWarning("Grup bulunamadı. GroupId: {GroupId}", groupId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grup getirilemedi. GroupId: {GroupId}", groupId);
                throw new InvalidOperationException($"Grup {groupId} alınamadı", ex);
            }
            finally
            {
                if (client != null)
                {
                    _graphServiceFactory.DisposeClient(client);
                }
            }
        }

        // 3. Birden fazla ID ile grupları getir (parametrik select/expand)
        public async Task<List<GroupDto>> GetGroupsByIdsAsync(List<string> groupIds, GraphQueryOptions? queryOptions = null)
        {
            GraphServiceClient? client = null;
            try
            {
                var resolvedQueryOptions = queryOptions ?? new GraphQueryOptions();
                
                _logger.LogInformation("Birden fazla grup getiriliyor. Sayı: {Count}, Select: {@Select}", 
                    groupIds.Count, resolvedQueryOptions.Select);

                client = await _graphServiceFactory.CreateSystemClientAsync(_azureAdConfiguration);

                var result = new List<GroupDto>();

                // Her grubu paralel olarak getir
                var tasks = groupIds.Select(async groupId =>
                {
                    try
                    {
                        var group = await client.Groups[groupId].GetAsync(config =>
                        {
                            config.QueryParameters.Select = resolvedQueryOptions.GetGroupSelectOrDefault();
                            
                            if (resolvedQueryOptions.Expand?.Any() == true)
                            {
                                config.QueryParameters.Expand = resolvedQueryOptions.Expand;
                            }
                        });
                        return group != null ? MapGroupToDto(group) : null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Grup getirilemedi: {GroupId}", groupId);
                        return null;
                    }
                }).ToList();

                var groups = await Task.WhenAll(tasks);
                result.AddRange(groups.Where(g => g != null).Select(g => g!));

                _logger.LogInformation("Toplam {Count} grup getirildi", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Birden fazla grup getirilemedi");
                throw new InvalidOperationException("Grup listesi alınamadı", ex);
            }
            finally
            {
                if (client != null)
                {
                    _graphServiceFactory.DisposeClient(client);
                }
            }
        }

        // 4. Grup arama (parametrik select/expand)
        public async Task<List<GroupDto>> SearchGroupsAsync(string searchTerm, int? top = null, GraphQueryOptions? queryOptions = null)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                throw new ArgumentException("Arama terimi boş olamaz", nameof(searchTerm));
            }

            // Microsoft Graph search formatını kontrol et ve düzelt
            string formattedSearchTerm = searchTerm;
            if (!searchTerm.Contains(':'))
            {
                // Eğer property:value formatında değilse, displayName araması yap
                formattedSearchTerm = $"displayName:{searchTerm}";
            }

            var filter = new GroupFilterOptions
            {
                Search = formattedSearchTerm,
                Top = top ?? 50,
                QueryOptions = queryOptions
            };

            return await GetAllGroupsAsync(filter);
        }

        // 5. Bir gruba üye userları getiren (parametrik select/expand)
        public async Task<List<UserDto>> GetGroupMembersAsync(string groupId, GraphQueryOptions? queryOptions = null)
        {
            GraphServiceClient? client = null;
            try
            {
                _logger.LogInformation("Grup üyeleri getiriliyor. GroupId: {GroupId}", groupId);

                client = await _graphServiceFactory.CreateSystemClientAsync(_azureAdConfiguration);

                return await GetGroupMembersInternalAsync(client, groupId, queryOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grup üyeleri getirilemedi. GroupId: {GroupId}", groupId);
                throw new InvalidOperationException($"Grup {groupId} üyeleri alınamadı", ex);
            }
            finally
            {
                if (client != null)
                {
                    _graphServiceFactory.DisposeClient(client);
                }
            }
        }

        // 6. Birden fazla grup ile bu gruba ait kullanıcıları getiren (parametrik select/expand)
        public async Task<Dictionary<string, List<UserDto>>> GetMultipleGroupMembersAsync(List<string> groupIds, GraphQueryOptions? queryOptions = null)
        {
            GraphServiceClient? client = null;
            var result = new Dictionary<string, List<UserDto>>();

            try
            {
                _logger.LogInformation("Birden fazla grup üyesi getiriliyor. Grup sayısı: {Count}", groupIds.Count);

                client = await _graphServiceFactory.CreateSystemClientAsync(_azureAdConfiguration);

                // Her grup için paralel olarak üyeleri getir
                var tasks = groupIds.Select(async groupId =>
                {
                    try
                    {
                        var members = await GetGroupMembersInternalAsync(client, groupId, queryOptions);
                        return new { GroupId = groupId, Members = members };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Grup {GroupId} üyeleri getirilemedi", groupId);
                        return new { GroupId = groupId, Members = new List<UserDto>() };
                    }
                }).ToList();

                var results = await Task.WhenAll(tasks);

                foreach (var groupResult in results)
                {
                    result[groupResult.GroupId] = groupResult.Members;
                }

                _logger.LogInformation("Toplam {GroupCount} grup için üye listesi getirildi", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Birden fazla grup üyesi getirilemedi");
                throw new InvalidOperationException("Grup üye listeleri alınamadı", ex);
            }
            finally
            {
                if (client != null)
                {
                    _graphServiceFactory.DisposeClient(client);
                }
            }
        }

        // 7. Grup sahiplerini getir (parametrik select/expand)
        public async Task<List<UserDto>> GetGroupOwnersAsync(string groupId, GraphQueryOptions? queryOptions = null)
        {
            GraphServiceClient? client = null;
            try
            {
                var resolvedQueryOptions = queryOptions ?? new GraphQueryOptions();
                
                _logger.LogInformation("Grup sahipleri getiriliyor. GroupId: {GroupId}, Select: {@Select}", 
                    groupId, resolvedQueryOptions.Select);

                client = await _graphServiceFactory.CreateSystemClientAsync(_azureAdConfiguration);

                var allOwners = new List<DirectoryObject>();
                string? nextPageUrl = null;

                do
                {
                    DirectoryObjectCollectionResponse? owners;
                    
                    if (nextPageUrl != null)
                    {
                        owners = await client.Groups[groupId].Owners.WithUrl(nextPageUrl).GetAsync();
                    }
                    else
                    {
                        owners = await client.Groups[groupId].Owners.GetAsync(config =>
                        {
                            config.QueryParameters.Select = resolvedQueryOptions.GetUserSelectOrDefault();
                            
                            if (resolvedQueryOptions.Expand?.Any() == true)
                            {
                                config.QueryParameters.Expand = resolvedQueryOptions.Expand;
                            }
                        });
                    }

                    if (owners?.Value != null)
                    {
                        allOwners.AddRange(owners.Value);
                        nextPageUrl = owners.OdataNextLink;
                    }
                    else
                    {
                        break;
                    }

                } while (nextPageUrl != null);

                // Sadece User olan sahipleri al
                var users = allOwners.OfType<User>().ToList();
                var result = users.Select(MapUserToDto).ToList();

                _logger.LogInformation("Grup {GroupId} için {Count} sahip getirildi", groupId, result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grup sahipleri getirilemedi. GroupId: {GroupId}", groupId);
                throw new InvalidOperationException($"Grup {groupId} sahipleri alınamadı", ex);
            }
            finally
            {
                if (client != null)
                {
                    _graphServiceFactory.DisposeClient(client);
                }
            }
        }

        // 8. Grup üye sayısını getir
        public async Task<int> GetGroupMemberCountAsync(string groupId)
        {
            GraphServiceClient? client = null;
            try
            {
                _logger.LogInformation("Grup üye sayısı getiriliyor. GroupId: {GroupId}", groupId);

                client = await _graphServiceFactory.CreateSystemClientAsync(_azureAdConfiguration);

                // Üyeleri sadece ID ile getir (performans için)
                var allMembers = new List<DirectoryObject>();
                string? nextPageUrl = null;
                int totalCount = 0;

                do
                {
                    DirectoryObjectCollectionResponse? members;
                    
                    if (nextPageUrl != null)
                    {
                        members = await client.Groups[groupId].Members.WithUrl(nextPageUrl).GetAsync();
                    }
                    else
                    {
                        members = await client.Groups[groupId].Members.GetAsync(config =>
                        {
                            config.QueryParameters.Select = new[] { "id" };
                        });
                    }

                    if (members?.Value != null)
                    {
                        totalCount += members.Value.Count;
                        nextPageUrl = members.OdataNextLink;
                    }
                    else
                    {
                        break;
                    }

                } while (nextPageUrl != null);

                _logger.LogInformation("Grup {GroupId} için üye sayısı: {Count}", groupId, totalCount);
                return totalCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grup üye sayısı getirilemedi. GroupId: {GroupId}", groupId);
                throw new InvalidOperationException($"Grup {groupId} üye sayısı alınamadı", ex);
            }
            finally
            {
                if (client != null)
                {
                    _graphServiceFactory.DisposeClient(client);
                }
            }
        }

        // 9. Kullanıcının grup üyesi olup olmadığını kontrol et
        public async Task<bool> IsUserGroupMemberAsync(string groupId, string userId)
        {
            GraphServiceClient client = null;
            try
            {
                _logger.LogInformation("Grup üyeliği kontrol ediliyor. GroupId: {GroupId}, UserId: {UserId}", groupId, userId);

                client = await _graphServiceFactory.CreateSystemClientAsync(_azureAdConfiguration);

                // Grup üyelerinden direkt control et
                try
                {
                    // Groups/[groupId]/members/[userId] endpoint'ini kullan
                    var member = await client.Groups[groupId].Members[userId].GraphGroup.GetAsync();
                    var isMember = member != null;
            
                    _logger.LogInformation("Grup üyelik durumu - GroupId: {GroupId}, UserId: {UserId}, IsMember: {IsMember}", 
                        groupId, userId, isMember);
                    return isMember;
                }
                catch (Microsoft.Graph.Models.ODataErrors.ODataError ex) when (ex.ResponseStatusCode == 404)
                {
                    // 404 hatası = kullanıcı bu grubun üyesi değil
                    _logger.LogInformation("Kullanıcı grup üyesi değil - GroupId: {GroupId}, UserId: {UserId}", 
                        groupId, userId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grup üyeliği kontrol edilemedi. GroupId: {GroupId}, UserId: {UserId}", groupId, userId);
                throw new InvalidOperationException($"Grup {groupId} üyeliği kontrol edilemedi", ex);
            }
            finally
            {
                if (client != null)
                {
                    _graphServiceFactory.DisposeClient(client);
                }
            }
        }

        // 10. Alt grupları getir (parametrik select/expand)
        public async Task<List<GroupDto>> GetSubGroupsAsync(string groupId, GraphQueryOptions? queryOptions = null)
        {
            GraphServiceClient? client = null;
            try
            {
                var resolvedQueryOptions = queryOptions ?? new GraphQueryOptions();
                
                _logger.LogInformation("Alt gruplar getiriliyor. GroupId: {GroupId}, Select: {@Select}", 
                    groupId, resolvedQueryOptions.Select);

                client = await _graphServiceFactory.CreateSystemClientAsync(_azureAdConfiguration);

                var allMembers = new List<DirectoryObject>();
                string? nextPageUrl = null;

                do
                {
                    DirectoryObjectCollectionResponse? members;
                    
                    if (nextPageUrl != null)
                    {
                        members = await client.Groups[groupId].Members.WithUrl(nextPageUrl).GetAsync();
                    }
                    else
                    {
                        members = await client.Groups[groupId].Members.GetAsync(config =>
                        {
                            config.QueryParameters.Select = resolvedQueryOptions.GetGroupSelectOrDefault();
                            
                            if (resolvedQueryOptions.Expand?.Any() == true)
                            {
                                config.QueryParameters.Expand = resolvedQueryOptions.Expand;
                            }
                        });
                    }

                    if (members?.Value != null)
                    {
                        allMembers.AddRange(members.Value);
                        nextPageUrl = members.OdataNextLink;
                    }
                    else
                    {
                        break;
                    }

                } while (nextPageUrl != null);

                // Sadece Group olan üyeleri al
                var subGroups = allMembers.OfType<Group>().ToList();
                var result = subGroups.Select(MapGroupToDto).ToList();

                _logger.LogInformation("Grup {GroupId} için {Count} alt grup getirildi", groupId, result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Alt gruplar getirilemedi. GroupId: {GroupId}", groupId);
                throw new InvalidOperationException($"Grup {groupId} alt grupları alınamadı", ex);
            }
            finally
            {
                if (client != null)
                {
                    _graphServiceFactory.DisposeClient(client);
                }
            }
        }

        // 11. Üst grupları getir (parametrik select/expand)
        public async Task<List<GroupDto>> GetParentGroupsAsync(string groupId, GraphQueryOptions? queryOptions = null)
        {
            GraphServiceClient? client = null;
            try
            {
                var resolvedQueryOptions = queryOptions ?? new GraphQueryOptions();
                
                _logger.LogInformation("Üst gruplar getiriliyor. GroupId: {GroupId}, Select: {@Select}", 
                    groupId, resolvedQueryOptions.Select);

                client = await _graphServiceFactory.CreateSystemClientAsync(_azureAdConfiguration);

                var allParents = new List<DirectoryObject>();
                string? nextPageUrl = null;

                do
                {
                    DirectoryObjectCollectionResponse? memberOf;
                    
                    if (nextPageUrl != null)
                    {
                        memberOf = await client.Groups[groupId].MemberOf.WithUrl(nextPageUrl).GetAsync();
                    }
                    else
                    {
                        memberOf = await client.Groups[groupId].MemberOf.GetAsync(config =>
                        {
                            config.QueryParameters.Select = resolvedQueryOptions.GetGroupSelectOrDefault();
                            
                            if (resolvedQueryOptions.Expand?.Any() == true)
                            {
                                config.QueryParameters.Expand = resolvedQueryOptions.Expand;
                            }
                        });
                    }

                    if (memberOf?.Value != null)
                    {
                        allParents.AddRange(memberOf.Value);
                        nextPageUrl = memberOf.OdataNextLink;
                    }
                    else
                    {
                        break;
                    }

                } while (nextPageUrl != null);

                // Sadece Group olan üst grupları al
                var parentGroups = allParents.OfType<Group>().ToList();
                var result = parentGroups.Select(MapGroupToDto).ToList();

                _logger.LogInformation("Grup {GroupId} için {Count} üst grup getirildi", groupId, result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Üst gruplar getirilemedi. GroupId: {GroupId}", groupId);
                throw new InvalidOperationException($"Grup {groupId} üst grupları alınamadı", ex);
            }
            finally
            {
                if (client != null)
                {
                    _graphServiceFactory.DisposeClient(client);
                }
            }
        }

        // Helper method - GetGroupMembersAsync'in internal versiyonu (parametrik select/expand)
        private async Task<List<UserDto>> GetGroupMembersInternalAsync(GraphServiceClient client, string groupId, GraphQueryOptions? queryOptions = null)
        {
            var resolvedQueryOptions = queryOptions ?? new GraphQueryOptions();
            var allMembers = new List<DirectoryObject>();
            string? nextPageUrl = null;

            do
            {
                DirectoryObjectCollectionResponse? members;
                
                if (nextPageUrl != null)
                {
                    members = await client.Groups[groupId].Members.WithUrl(nextPageUrl).GetAsync();
                }
                else
                {
                    members = await client.Groups[groupId].Members.GetAsync(config =>
                    {
                        config.QueryParameters.Select = resolvedQueryOptions.GetUserSelectOrDefault();
                        
                        if (resolvedQueryOptions.Expand?.Any() == true)
                        {
                            config.QueryParameters.Expand = resolvedQueryOptions.Expand;
                        }
                    });
                }

                if (members?.Value != null)
                {
                    allMembers.AddRange(members.Value);
                    nextPageUrl = members.OdataNextLink;
                }
                else
                {
                    break;
                }

            } while (nextPageUrl != null);

            var users = allMembers.OfType<User>().ToList();
            var result = users.Select(MapUserToDto).ToList();
            
            _logger.LogInformation("Grup {GroupId} için {Count} üye getirildi", groupId, result.Count);
            return result;
        }

        // Mapping metodları
        private static GroupDto MapGroupToDto(Group group)
        {
            return new GroupDto
            {
                Id = group.Id ?? string.Empty,
                DisplayName = group.DisplayName ?? string.Empty,
                Mail = group.Mail ?? string.Empty,
                MailNickname = group.MailNickname ?? string.Empty,
                Description = group.Description ?? string.Empty,
                GroupTypes = group.GroupTypes?.ToList() ?? new List<string>(),
                SecurityEnabled = group.SecurityEnabled ?? false,
                MailEnabled = group.MailEnabled ?? false,
            };
        }

        private static UserDto MapUserToDto(User user)
        {
            var userDto = new UserDto
            {
                Id = user.Id ?? string.Empty,
                DisplayName = user.DisplayName ?? string.Empty,
                Mail = user.Mail ?? string.Empty,
                UserPrincipalName = user.UserPrincipalName ?? string.Empty,
                JobTitle = user.JobTitle ?? string.Empty,
                Department = user.Department ?? string.Empty,
                OfficeLocation = user.OfficeLocation ?? string.Empty,
                MobilePhone = user.MobilePhone ?? string.Empty,
                BusinessPhones = user.BusinessPhones?.ToList() ?? new List<string>(),
                AccountEnabled = user.AccountEnabled ?? false,
            };

            // Expand edilen grup bilgilerini işle
            if (user.MemberOf?.Any() == true)
            {
                userDto.Groups = user.MemberOf
                    .OfType<Group>()
                    .Select(MapGroupToDto)
                    .ToList();
            }

            return userDto;
        }
    }
}