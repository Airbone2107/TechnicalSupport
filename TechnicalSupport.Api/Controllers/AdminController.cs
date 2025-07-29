using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechnicalSupport.Api.Common;
using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Features.Admin.DTOs;
using TechnicalSupport.Application.Interfaces;

namespace TechnicalSupport.Api.Controllers
{
    [ApiController]
    [Route("admin")]
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
                return NotFound(ApiResponse.Fail($"User with Id {userId} not found."));
            }
            return Ok(ApiResponse.Success(user));
        }

        [HttpPut("users/{userId}")]
        public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserByAdminModel model)
        {
            var updatedUser = await _adminService.UpdateUserAsync(userId, model);
            if (updatedUser == null)
            {
                return NotFound(ApiResponse.Fail($"User with Id {userId} not found."));
            }
            return Ok(ApiResponse.Success(updatedUser, "User updated successfully."));
        }
    }
}