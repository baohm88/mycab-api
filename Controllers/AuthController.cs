using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyCabs.Api.DTOs;
using MyCabs.Api.Models;
using MyCabs.Api.Services;
using System.IdentityModel.Tokens.Jwt;

namespace MyCabs.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Đăng ký tài khoản mới (User, Company, Driver, Admin)
        /// </summary>
        /// <param name="dto">Thông tin đăng ký</param>
        /// <returns>Thông báo đăng ký thành công</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse<object>.Fail(errors, "Validation failed"));
            }

            try
            {
                await _authService.RegisterAsync(dto);
                return Created(string.Empty, ApiResponse<object>.Ok(null, "Registered successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ApiResponse<object>.Fail(ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Register error");
                return StatusCode(500, ApiResponse<object>.Fail("Internal server error"));
            }
        }

        /// <summary>
        /// Đăng nhập và lấy JWT token
        /// </summary>
        /// <param name="dto">Thông tin đăng nhập</param>
        /// <returns>JWT token</returns>
        /// 
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse<object>.Fail(errors, "Validation failed"));
            }

            try
            {
                var (user, token) = await _authService.LoginAsync(dto);

                var userResponse = new
                {
                    id = user.Id,
                    email = user.Email,
                    role = user.Role.ToString(),
                    isApproved = user.IsApproved,
                    createdAt = user.CreatedAt
                };

                return Ok(ApiResponse<object>.Ok(new { token, user = userResponse }, "Login successful"));
            }
            catch (UnauthorizedAccessException ex)
            {
                if (ex.Message.Contains("not approved", StringComparison.OrdinalIgnoreCase))
                    return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(ex.Message));

                return Unauthorized(ApiResponse<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error");
                return StatusCode(500, ApiResponse<object>.Fail("Internal server error"));
            }
        }


        /// <summary>
        /// Đổi mật khẩu (yêu cầu đã đăng nhập)
        /// </summary>
        /// <param name="dto">Mật khẩu hiện tại và mật khẩu mới</param>
        [Authorize]
        [HttpPut("change-password")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));

            try
            {
                await _authService.ChangePasswordAsync(userId, dto);
                return Ok(ApiResponse<object>.Ok(null, "Password changed"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Change password error");
                return StatusCode(500, ApiResponse<object>.Fail("Internal server error"));
            }
        }

        /// <summary>
        /// Cập nhật thông tin tài khoản (yêu cầu đã đăng nhập)
        /// </summary>
        /// <param name="dto">Thông tin cần cập nhật</param>
        [Authorize]
        [HttpPut("update-account")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateAccount([FromBody] UpdateAccountDto dto)
        {
            var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));

            try
            {
                await _authService.UpdateAccountAsync(userId, dto);
                return Ok(ApiResponse<object>.Ok(null, "Account updated"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update account error");
                return StatusCode(500, ApiResponse<object>.Fail("Internal server error"));
            }
        }
    }
}
