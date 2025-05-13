using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureExternalDirectory.Domain;
using AzureExternalDirectory.Infrastructure.GraphService.Factory;
using AzureExternalDirectory.Infrastructure.GraphService.Helper;
using AzureExternalDirectory.Infrastructure.GraphService.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace AzureExternalDirectory.Application.UserService
{
    
    public class UserGraphService : IUserGraphService
    {
        private readonly IGraphServiceFactory _graphServiceFactory;
        private readonly AzureAdConfiguration _azureAdConfiguration;
        private readonly ILogger<UserGraphService> _logger;

        public UserGraphService(
            IGraphServiceFactory graphServiceFactory,
            IOptions<AzureAdConfiguration> azureAdConfiguration,
            ILogger<UserGraphService> logger)
        {
            _graphServiceFactory = graphServiceFactory;
            _azureAdConfiguration = azureAdConfiguration.Value;
            _logger = logger;
        }

        // 1. Sistemdeki bütün userları veya belirli bir filtreye göre çeken (parametrik select/expand)
        public async Task<List<UserDto>> GetAllUsersAsync(UserFilterOptions? filter = null, GraphQueryOptions? queryOptions = null)
        {
            GraphServiceClient? client = null;
            try
            {
                // QueryOptions öncelik sırası: parametre > filter.QueryOptions > default
                var resolvedQueryOptions = queryOptions ?? filter?.GetQueryOptionsOrDefault() ?? new GraphQueryOptions();
                
                _logger.LogInformation("Kullanıcılar getiriliyor. Filter: {@Filter}, Select: {@Select}, Expand: {@Expand}", 
                    filter, resolvedQueryOptions.Select, resolvedQueryOptions.Expand);

                client = await _graphServiceFactory.CreateSystemClientAsync(_azureAdConfiguration);

                var allUsers = new List<User>();
                string? nextPageUrl = null;

                do
                {
                    UserCollectionResponse? users;
                    
                    if (nextPageUrl != null)
                    {
                        users = await client.Users.WithUrl(nextPageUrl).GetAsync();
                    }
                    else
                    {
                        users = await client.Users.GetAsync(config =>
                        {
                            // Select alanları uygula
                            var selectFields = resolvedQueryOptions.GetUserSelectOrDefault();
                            config.QueryParameters.Select = selectFields;
                            
                            // Expand alanları varsa uygula
                            if (resolvedQueryOptions.HasExpand())
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

                    if (users?.Value != null)
                    {
                        allUsers.AddRange(users.Value);
                        nextPageUrl = users.OdataNextLink;
                    }
                    else
                    {
                        break;
                    }

                    if (filter?.Top.HasValue == true && allUsers.Count >= filter.Top.Value)
                    {
                        allUsers = allUsers.Take(filter.Top.Value).ToList();
                        break;
                    }

                } while (nextPageUrl != null);

                var result = allUsers.Select(MapUserToDto).ToList();
                
                _logger.LogInformation("Toplam {Count} kullanıcı getirildi", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcılar getirilemedi");
                throw new InvalidOperationException("Kullanıcı listesi alınamadı", ex);
            }
            finally
            {
                if (client != null)
                {
                    _graphServiceFactory.DisposeClient(client);
                }
            }
        }

        // 2. Sistemdeki userları çekerken üye olduğu grupları da getiren (parametrik select/expand)
        public async Task<List<UserWithGroupsDto>> GetUsersWithGroupsAsync(UserFilterOptions? filter = null, GraphQueryOptions? userQueryOptions = null, GraphQueryOptions? groupQueryOptions = null)
        {
            GraphServiceClient? client = null;
            try
            {
                _logger.LogInformation("Kullanıcılar gruplarıyla birlikte getiriliyor");

                client = await _graphServiceFactory.CreateSystemClientAsync(_azureAdConfiguration);

                // Önce kullanıcıları getir (parametrik select ile)
                var users = await GetAllUsersAsync(filter, userQueryOptions);
                var result = new List<UserWithGroupsDto>();
                
                var resolvedGroupQueryOptions = groupQueryOptions ?? new GraphQueryOptions();

                // Her kullanıcı için gruplarını getir
                foreach (var user in users)
                {
                    try
                    {
                        var userGroups = await client.Users[user.Id].MemberOf.GetAsync(config =>
                        {
                            config.QueryParameters.Select = resolvedGroupQueryOptions.GetGroupSelectOrDefault();
                            
                            if (resolvedGroupQueryOptions.Expand?.Any() == true)
                            {
                                config.QueryParameters.Expand = resolvedGroupQueryOptions.Expand;
                            }
                        });

                        var userWithGroups = new UserWithGroupsDto
                        {
                            User = user,
                            Groups = userGroups?.Value?
                                .OfType<Group>()
                                .Select(MapGroupToDto)
                                .ToList() ?? new List<GroupDto>()
                        };

                        result.Add(userWithGroups);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Kullanıcı {UserId} için gruplar getirilemedi", user.Id);
                        result.Add(new UserWithGroupsDto
                        {
                            User = user,
                            Groups = new List<GroupDto>()
                        });
                    }
                }

                _logger.LogInformation("Toplam {Count} kullanıcı gruplarıyla birlikte getirildi", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcılar gruplarıyla getirilemedi");
                throw new InvalidOperationException("Kullanıcı ve grup listesi alınamadı", ex);
            }
            finally
            {
                if (client != null)
                {
                    _graphServiceFactory.DisposeClient(client);
                }
            }
        }

        // 3. ID ile kullanıcı getir (parametrik select/expand)
        public async Task<UserDto?> GetUserByIdAsync(string userId, GraphQueryOptions? queryOptions = null)
        {
            GraphServiceClient? client = null;
            try
            {
                var resolvedQueryOptions = queryOptions ?? new GraphQueryOptions();
                
                _logger.LogInformation("Kullanıcı getiriliyor. UserId: {UserId}, Select: {@Select}", 
                    userId, resolvedQueryOptions.Select);

                client = await _graphServiceFactory.CreateSystemClientAsync(_azureAdConfiguration);

                var user = await client.Users[userId].GetAsync(config =>
                {
                    config.QueryParameters.Select = resolvedQueryOptions.GetUserSelectOrDefault();
                    
                    if (resolvedQueryOptions.Expand?.Any() == true)
                    {
                        config.QueryParameters.Expand = resolvedQueryOptions.Expand;
                    }
                });

                if (user == null)
                {
                    _logger.LogWarning("Kullanıcı bulunamadı. UserId: {UserId}", userId);
                    return null;
                }

                var result = MapUserToDto(user);
                _logger.LogInformation("Kullanıcı getirildi: {DisplayName}", result.DisplayName);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı getirilemedi. UserId: {UserId}", userId);
                throw new InvalidOperationException($"Kullanıcı {userId} alınamadı", ex);
            }
            finally
            {
                if (client != null)
                {
                    _graphServiceFactory.DisposeClient(client);
                }
            }
        }

        // 4. Birden fazla ID ile kullanıcıları getir (parametrik select/expand)
        public async Task<List<UserDto>> GetUsersByIdsAsync(List<string> userIds, GraphQueryOptions? queryOptions = null)
        {
            GraphServiceClient? client = null;
            try
            {
                var resolvedQueryOptions = queryOptions ?? new GraphQueryOptions();
                
                _logger.LogInformation("Birden fazla kullanıcı getiriliyor. Sayı: {Count}, Select: {@Select}", 
                    userIds.Count, resolvedQueryOptions.Select);

                client = await _graphServiceFactory.CreateSystemClientAsync(_azureAdConfiguration);

                var result = new List<UserDto>();

                // Her kullanıcıyı paralel olarak getir
                var tasks = userIds.Select(async userId =>
                {
                    try
                    {
                        var user = await client.Users[userId].GetAsync(config =>
                        {
                            config.QueryParameters.Select = resolvedQueryOptions.GetUserSelectOrDefault();
                            
                            if (resolvedQueryOptions.Expand?.Any() == true)
                            {
                                config.QueryParameters.Expand = resolvedQueryOptions.Expand;
                            }
                        });
                        return user != null ? MapUserToDto(user) : null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Kullanıcı getirilemedi: {UserId}", userId);
                        return null;
                    }
                });

                var users = await Task.WhenAll(tasks);
                result.AddRange(users.Where(u => u != null).Select(u => u!));

                _logger.LogInformation("Toplam {Count} kullanıcı getirildi", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Birden fazla kullanıcı getirilemedi");
                throw new InvalidOperationException("Kullanıcı listesi alınamadı", ex);
            }
            finally
            {
                if (client != null)
                {
                    _graphServiceFactory.DisposeClient(client);
                }
            }
        }

        // 5. Kullanıcı arama (parametrik select/expand)
        public async Task<List<UserDto>> SearchUsersAsync(string searchTerm, int? top = null, GraphQueryOptions? queryOptions = null)
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

            var filter = new UserFilterOptions
            {
                Search = formattedSearchTerm,
                Top = top ?? 50,
                QueryOptions = queryOptions
            };

            return await GetAllUsersAsync(filter);
        }

        // 6. Kullanıcının üye olduğu grupları getir (parametrik select/expand)
        public async Task<List<GroupDto>> GetUserGroupsAsync(string userId, GraphQueryOptions? queryOptions = null)
        {
            GraphServiceClient? client = null;
            try
            {
                var resolvedQueryOptions = queryOptions ?? new GraphQueryOptions();
                
                _logger.LogInformation("Kullanıcı grupları getiriliyor. UserId: {UserId}, Select: {@Select}", 
                    userId, resolvedQueryOptions.GetGroupSelectOrDefault());

                client = await _graphServiceFactory.CreateSystemClientAsync(_azureAdConfiguration);

                var allGroups = new List<Group>();
                string? nextPageUrl = null;

                do
                {
                    DirectoryObjectCollectionResponse? memberOf;
                    
                    if (nextPageUrl != null)
                    {
                        memberOf = await client.Users[userId].MemberOf.WithUrl(nextPageUrl).GetAsync();
                    }
                    else
                    {
                        memberOf = await client.Users[userId].MemberOf.GetAsync(config =>
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
                        var groups = memberOf.Value.OfType<Group>().ToList();
                        allGroups.AddRange(groups);
                        nextPageUrl = memberOf.OdataNextLink;
                    }
                    else
                    {
                        break;
                    }

                } while (nextPageUrl != null);

                var result = allGroups.Select(MapGroupToDto).ToList();
                
                _logger.LogInformation("Kullanıcı {UserId} için {Count} grup getirildi", userId, result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı grupları getirilemedi. UserId: {UserId}", userId);
                throw new InvalidOperationException($"Kullanıcı {userId} grupları alınamadı", ex);
            }
            finally
            {
                if (client != null)
                {
                    _graphServiceFactory.DisposeClient(client);
                }
            }
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

        // Bu metodlar artık gereksiz çünkü GraphQueryOptions.GetUserSelectOrDefault() ve 
        // GraphQueryOptions.GetGroupSelectOrDefault() metodları kullanılıyor
    }
}