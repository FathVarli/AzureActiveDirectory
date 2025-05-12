using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using AzureExternalDirectory.Application.AuthService;
using AzureExternalDirectory.Infrastructure.GraphService.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AzureExternalDirectory.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthGraphController : ControllerBase
    {
        private readonly IGraphAuthService _graphAuthService;
        private readonly ILogger<AuthGraphController> _logger;

        public AuthGraphController(IGraphAuthService graphAuthService, ILogger<AuthGraphController> logger)
        {
            _graphAuthService = graphAuthService;
            _logger = logger;
        }
        
        /// <summary>
        /// Kullanıcı kimlik bilgilerini doğrular
        /// </summary>
        /// <param name="request">Giriş bilgileri</param>
        /// <returns>Doğrulama sonucu</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Giriş denemesi: {Username}", request.Username);

                var credentials = new UserCredentialsDto
                {
                    Username = request.Username,
                    Password = request.Password
                };

                var result = await _graphAuthService.AuthenticateUserAsync(credentials);

                if (result.IsSuccess)
                {
                    return Ok(new LoginResponse
                    {
                        Success = true,
                        Message = result.Message,
                        UserId = result.UserId,
                        Username = result.Username,
                        AuthenticationTime = result.AuthenticationTime
                    });
                }
                else
                {
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = result.Message,
                        ErrorCode = result.ErrorCode,
                        ErrorDetails = result.ErrorDetails
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Giriş işlemi sırasında hata oluştu");
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "Internal server error occurred",
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Kullanıcıyı doğrular ve bilgilerini getirir
        /// </summary>
        /// <param name="request">Giriş bilgileri</param>
        /// <returns>Doğrulama sonucu ve kullanıcı bilgileri</returns>
        [HttpPost("login-with-info")]
        public async Task<IActionResult> LoginWithUserInfo([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Detaylı giriş denemesi: {Username}", request.Username);

                var credentials = new UserCredentialsDto
                {
                    Username = request.Username,
                    Password = request.Password
                };

                var result = await _graphAuthService.AuthenticateAndGetUserInfoAsync(credentials);

                if (result.IsSuccess)
                {
                    return Ok(new DetailedLoginResponse
                    {
                        Success = true,
                        Message = result.Message,
                        UserId = result.UserId,
                        Username = result.Username,
                        AuthenticationTime = result.AuthenticationTime,
                        UserInfo = result.UserInfo,
                        UserGroups = result.UserGroups
                    });
                }
                else
                {
                    return Unauthorized(new DetailedLoginResponse
                    {
                        Success = false,
                        Message = result.Message,
                        ErrorCode = result.ErrorCode,
                        ErrorDetails = result.ErrorDetails
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detaylı giriş işlemi sırasında hata oluştu");
                return StatusCode(500, new DetailedLoginResponse
                {
                    Success = false,
                    Message = "Internal server error occurred",
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Kullanıcı kimlik bilgilerini doğrular (sadece boolean sonuç)
        /// </summary>
        /// <param name="request">Giriş bilgileri</param>
        /// <returns>Doğrulama durumu</returns>
        [HttpPost("validate")]
        public async Task<IActionResult> ValidateCredentials([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Kimlik bilgisi doğrulama: {Username}", request.Username);

                var credentials = new UserCredentialsDto
                {
                    Username = request.Username,
                    Password = request.Password
                };

                var isValid = await _graphAuthService.ValidateUserCredentialsAsync(credentials);

                return Ok(new ValidationResponse
                {
                    Username = request.Username,
                    IsValid = isValid,
                    ValidatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kimlik bilgisi doğrulama sırasında hata oluştu");
                return StatusCode(500, new ValidationResponse
                {
                    Username = request.Username,
                    IsValid = false,
                    Error = "Validation could not be completed"
                });
            }
        }

        /// <summary>
        /// Doğrulanmış kullanıcının bilgilerini getirir
        /// </summary>
        /// <param name="request">Giriş bilgileri</param>
        /// <returns>Kullanıcı bilgileri</returns>
        [HttpPost("user-info")]
        public async Task<IActionResult> GetUserInfo([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Kullanıcı bilgisi isteniyor: {Username}", request.Username);

                var credentials = new UserCredentialsDto()
                {
                    Username = request.Username,
                    Password = request.Password
                };

                var userInfo = await _graphAuthService.GetAuthenticatedUserInfoAsync(credentials);

                if (userInfo != null)
                {
                    return Ok(new UserInfoResponse
                    {
                        Success = true,
                        UserInfo = userInfo
                    });
                }
                else
                {
                    return Unauthorized(new UserInfoResponse
                    {
                        Success = false,
                        Message = "Authentication failed or user not found"
                    });
                }
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new UserInfoResponse
                {
                    Success = false,
                    Message = "Invalid credentials"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı bilgisi alma sırasında hata oluştu");
                return StatusCode(500, new UserInfoResponse
                {
                    Success = false,
                    Message = "Internal server error occurred"
                });
            }
        }
        
        
    }
    
    public class LoginRequest
    {
        [Required(ErrorMessage = "Username is required")]
        [EmailAddress(ErrorMessage = "Please provide a valid email address")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(1, ErrorMessage = "Password cannot be empty")]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string UserId { get; set; }
        public string Username { get; set; }
        public DateTime? AuthenticationTime { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorDetails { get; set; }
    }

    public class DetailedLoginResponse : LoginResponse
    {
        public UserDto UserInfo { get; set; }
        public List<GroupDto> UserGroups { get; set; } = new List<GroupDto>();
    }

    public class ValidationResponse
    {
        public string Username { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public DateTime ValidatedAt { get; set; }
        public string Error { get; set; }
    }

    public class UserInfoResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public UserDto UserInfo { get; set; }
    }
}