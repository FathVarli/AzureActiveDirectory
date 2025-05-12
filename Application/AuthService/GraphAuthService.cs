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

namespace AzureExternalDirectory.Application.AuthService
{
    public class GraphAuthService : IGraphAuthService
    {
        private readonly IGraphServiceFactory _graphServiceFactory;
        private readonly AzureAdConfiguration _azureAdConfiguration;
        private readonly ILogger<GraphAuthService> _logger;

        public GraphAuthService(
            IGraphServiceFactory graphServiceFactory,
            IOptions<AzureAdConfiguration> azureAdConfiguration,
            ILogger<GraphAuthService> logger)
        {
            _graphServiceFactory = graphServiceFactory;
            _azureAdConfiguration = azureAdConfiguration.Value;
            _logger = logger;
        }

        /// <summary>
        /// Kullanıcı kimlik bilgilerini doğrular
        /// </summary>
        /// <param name="credentials">Kullanıcı kimlik bilgileri</param>
        /// <returns>Doğrulama sonucu</returns>
        public async Task<AuthenticationResult> AuthenticateUserAsync(UserCredentialsDto credentials)
        {
            var result = new AuthenticationResult
            {
                Username = credentials.Username,
                AuthenticationTime = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("Kullanıcı doğrulama başlatılıyor: {Username}", credentials.Username);

                // Client oluştur ve bağlantıyı test et
                GraphServiceClient? client = null;
                try
                {
                    client = await _graphServiceFactory.CreateUserClientAsync(
                        credentials.Username,
                        credentials.Password,
                        _azureAdConfiguration);

                    // Basit bir test isteği yaparak doğrulamayı kontrol et
                    var user = await client.Me.GetAsync();
                    
                    if (user != null)
                    {
                        result.IsSuccess = true;
                        result.UserId = user.Id;
                        result.Message = "Authentication successful";
                        _logger.LogInformation("Kullanıcı doğrulama başarılı: {Username}, UserId: {UserId}", 
                            credentials.Username, user.Id);
                    }
                    else
                    {
                        result.IsSuccess = false;
                        result.Message = "User information could not be retrieved";
                        _logger.LogWarning("Kullanıcı bilgileri alınamadı: {Username}", credentials.Username);
                    }
                }
                finally
                {
                    if (client != null)
                    {
                        _graphServiceFactory.DisposeClient(client);
                    }
                }
            }
            catch (Microsoft.Graph.Models.ODataErrors.ODataError ex) when (ex.Error?.Code == "invalid_grant")
            {
                result.IsSuccess = false;
                result.Message = "Invalid credentials";
                result.ErrorCode = "INVALID_CREDENTIALS";
                _logger.LogWarning("Geçersiz kimlik bilgileri: {Username}", credentials.Username);
            }
            catch (UnauthorizedAccessException ex)
            {
                result.IsSuccess = false;
                result.Message = "Authentication failed";
                result.ErrorCode = "UNAUTHORIZED";
                _logger.LogWarning(ex, "Yetkilendirme hatası: {Username}", credentials.Username);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = "Authentication error occurred";
                result.ErrorCode = "AUTH_ERROR";
                result.ErrorDetails = ex.Message;
                _logger.LogError(ex, "Doğrulama hatası: {Username}", credentials.Username);
            }

            return result;
        }

        /// <summary>
        /// Doğrulanmış kullanıcının bilgilerini getirir
        /// </summary>
        /// <param name="credentials">Kullanıcı kimlik bilgileri</param>
        /// <returns>Kullanıcı bilgileri</returns>
        public async Task<UserDto> GetAuthenticatedUserInfoAsync(UserCredentialsDto credentials)
        {
            GraphServiceClient? client = null;
            try
            {
                _logger.LogInformation("Doğrulanmış kullanıcı bilgileri alınıyor: {Username}", credentials.Username);

                client = await _graphServiceFactory.CreateUserClientAsync(
                    credentials.Username,
                    credentials.Password,
                    _azureAdConfiguration);

                var user = await client.Me.GetAsync(config =>
                {
                    config.QueryParameters.Select = GetUserSelectFields();
                });

                if (user == null)
                {
                    _logger.LogWarning("Kullanıcı bilgileri bulunamadı: {Username}", credentials.Username);
                    return null;
                }

                var userDto = MapUserToDto(user);
                _logger.LogInformation("Kullanıcı bilgileri başarıyla alındı: {Username}, {DisplayName}", 
                    credentials.Username, userDto.DisplayName);
                return userDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı bilgileri alınamadı: {Username}", credentials.Username);
                throw;
            }
            finally
            {
                if (client != null)
                {
                    _graphServiceFactory.DisposeClient(client);
                }
            }
        }

        /// <summary>
        /// Sadece kimlik bilgilerini doğrular (kullanıcı bilgilerini getirmez)
        /// </summary>
        /// <param name="credentials">Kullanıcı kimlik bilgileri</param>
        /// <returns>Doğrulama durumu</returns>
        public async Task<bool> ValidateUserCredentialsAsync(UserCredentialsDto credentials)
        {
            GraphServiceClient? client = null;
            try
            {
                _logger.LogInformation("Kimlik bilgileri doğrulanıyor: {Username}", credentials.Username);

                client = await _graphServiceFactory.CreateUserClientAsync(
                    credentials.Username,
                    credentials.Password,
                    _azureAdConfiguration);

                // Basit bir test isteği
                var user = await client.Me.GetAsync(config =>
                {
                    config.QueryParameters.Select = new[] { "id" };
                });

                var isValid = user != null;
                _logger.LogInformation("Kimlik bilgileri doğrulama sonucu: {Username}, Valid: {IsValid}", 
                    credentials.Username, isValid);
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kimlik bilgileri geçersiz: {Username}", credentials.Username);
                return false;
            }
            finally
            {
                if (client != null)
                {
                    _graphServiceFactory.DisposeClient(client);
                }
            }
        }

        /// <summary>
        /// Kullanıcıyı doğrular ve bilgilerini getirir
        /// </summary>
        /// <param name="credentials">Kullanıcı kimlik bilgileri</param>
        /// <returns>Doğrulama sonucu ve kullanıcı bilgileri</returns>
        public async Task<AuthenticationResult> AuthenticateAndGetUserInfoAsync(UserCredentialsDto credentials)
        {
            var result = new AuthenticationResult
            {
                Username = credentials.Username,
                AuthenticationTime = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("Kullanıcı doğrulama ve bilgi alma başlatılıyor: {Username}", credentials.Username);

                GraphServiceClient? client = null;
                try
                {
                    client = await _graphServiceFactory.CreateUserClientAsync(
                        credentials.Username,
                        credentials.Password,
                        _azureAdConfiguration);

                    // Kullanıcı bilgilerini detaylı olarak al
                    var user = await client.Me.GetAsync(config =>
                    {
                        config.QueryParameters.Select = GetUserSelectFields();
                    });

                    if (user != null)
                    {
                        result.IsSuccess = true;
                        result.UserId = user.Id;
                        result.Message = "Authentication successful";
                        result.UserInfo = MapUserToDto(user);

                        // Kullanıcının gruplarını da getir
                        try
                        {
                            var groups = await client.Me.MemberOf.GetAsync(config =>
                            {
                                config.QueryParameters.Select = new[] { "id", "displayName" };
                                config.QueryParameters.Top = 20;
                            });

                            if (groups?.Value != null)
                            {
                                result.UserGroups = groups.Value
                                    .OfType<Group>()
                                    .Select(g => new GroupDto
                                    {
                                        Id = g.Id ?? string.Empty,
                                        DisplayName = g.DisplayName ?? string.Empty
                                    })
                                    .ToList();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Kullanıcı grupları alınamadı: {Username}", credentials.Username);
                        }

                        _logger.LogInformation("Kullanıcı doğrulama ve bilgi alma başarılı: {Username}, {DisplayName}", 
                            credentials.Username, result.UserInfo.DisplayName);
                    }
                }
                finally
                {
                    if (client != null)
                    {
                        _graphServiceFactory.DisposeClient(client);
                    }
                }
            }
            catch (Microsoft.Graph.Models.ODataErrors.ODataError ex) when (ex.Error?.Code == "invalid_grant")
            {
                result.IsSuccess = false;
                result.Message = "Invalid credentials";
                result.ErrorCode = "INVALID_CREDENTIALS";
                _logger.LogWarning("Geçersiz kimlik bilgileri: {Username}", credentials.Username);
            }
            catch (UnauthorizedAccessException ex)
            {
                result.IsSuccess = false;
                result.Message = "Authentication failed";
                result.ErrorCode = "UNAUTHORIZED";
                _logger.LogWarning(ex, "Yetkilendirme hatası: {Username}", credentials.Username);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = "Authentication error occurred";
                result.ErrorCode = "AUTH_ERROR";
                result.ErrorDetails = ex.Message;
                _logger.LogError(ex, "Doğrulama hatası: {Username}", credentials.Username);
            }

            return result;
        }

        // Helper metodlar
        private static UserDto MapUserToDto(User user)
        {
            return new UserDto
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
                AccountEnabled = user.AccountEnabled ?? false
            };
        }

        private static string[] GetUserSelectFields()
        {
            return new[]
            {
                "id",
                "displayName",
                "mail",
                "userPrincipalName",
                "jobTitle",
                "department",
                "officeLocation",
                "mobilePhone",
                "businessPhones",
                "accountEnabled"
            };
        }
    }
}