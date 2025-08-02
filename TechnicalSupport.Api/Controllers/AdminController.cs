using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechnicalSupport.Api.Common;
using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Features.Admin.DTOs;
using TechnicalSupport.Application.Interfaces;

namespace TechnicalSupport.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] PaginationParams paginationParams)
        {
            var users = await _adminService.GetUsersAsync(paginationParams);
            return Ok(ApiResponse.Success(users));
        }

        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetUser(string userId)
        {
            var user = await _adminService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse.Fail("User not found."));
            }
            return Ok(ApiResponse.Success(user));
        }
        
        [HttpPut("users/{userId}")]
        public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserByAdminModel model)
        {
            var user = await _adminService.UpdateUserAsync(userId, model);
            if (user == null)
            {
                return NotFound(ApiResponse.Fail("User not found."));
            }
            return Ok(ApiResponse.Success(user, "User updated successfully."));
        }

        [HttpPut("users/{userId}/promote")]
        public async Task<IActionResult> PromoteUser(string userId, [FromBody] PromoteUserModel model)
        {
            var (success, message) = await _adminService.PromoteUserToTechnicianAsync(userId, model);
            if (!success)
            {
                return BadRequest(ApiResponse.Fail(message));
            }
            return Ok(ApiResponse.Success<object>(null, message));
        }

        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var (success, message) = await _adminService.DeleteUserAsync(userId);
            if (!success)
            {
                return BadRequest(ApiResponse.Fail(message));
            }
            return Ok(ApiResponse.Success<object>(null, message));
        }
    }
} 