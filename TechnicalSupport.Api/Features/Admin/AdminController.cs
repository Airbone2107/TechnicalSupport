using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechnicalSupport.Api.Common;
using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Features.Admin.Abstractions;
using TechnicalSupport.Application.Features.Admin.DTOs;
using TechnicalSupport.Application.Features.Tickets.DTOs;
using TechnicalSupport.Domain.Entities;

namespace TechnicalSupport.Api.Features.Admin
{
    /// <summary>
    /// Cung cấp các endpoint dành cho quản trị viên để quản lý người dùng và vai trò.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IMapper _mapper;

        /// <summary>
        /// Khởi tạo một instance mới của AdminController.
        /// </summary>
        public AdminController(IAdminService adminService, UserManager<ApplicationUser> userManager, IMapper mapper, RoleManager<IdentityRole> roleManager)
        {
            _adminService = adminService;
            _userManager = userManager;
            _mapper = mapper;
            _roleManager = roleManager;
        }

        /// <summary>
        /// Lấy danh sách người dùng trong hệ thống với bộ lọc và phân trang.
        /// </summary>
        /// <param name="filterParams">Các tham số để lọc và phân trang danh sách người dùng.</param>
        /// <returns>Một danh sách người dùng đã được phân trang.</returns>
        [HttpGet("users")]
        [Authorize(Policy = "ReadUsers")]
        public async Task<IActionResult> GetUsers([FromQuery] UserFilterParams filterParams)
        {
            var users = await _adminService.GetUsersAsync(filterParams);
            return Ok(ApiResponse.Success(users));
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một người dùng theo ID.
        /// </summary>
        /// <param name="userId">ID của người dùng cần lấy thông tin.</param>
        /// <returns>Thông tin chi tiết của người dùng hoặc lỗi Not Found nếu không tìm thấy.</returns>
        [HttpGet("users/{userId}")]
        [Authorize(Policy = "ReadUsers")]
        public async Task<IActionResult> GetUser(string userId)
        {
            var user = await _adminService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse.Fail("User not found."));
            }
            return Ok(ApiResponse.Success(user));
        }
        
        /// <summary>
        /// Cập nhật thông tin của một người dùng (thực hiện bởi quản trị viên).
        /// </summary>
        /// <param name="userId">ID của người dùng cần cập nhật.</param>
        /// <param name="model">Dữ liệu cập nhật.</param>
        /// <returns>Thông tin người dùng sau khi cập nhật hoặc lỗi Not Found nếu không tìm thấy.</returns>
        [HttpPut("users/{userId}")]
        [Authorize(Policy = "ManageUsers")]
        public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserByAdminModel model)
        {
            var user = await _adminService.UpdateUserAsync(userId, model);
            if (user == null)
            {
                return NotFound(ApiResponse.Fail("User not found."));
            }
            return Ok(ApiResponse.Success(user, "User updated successfully."));
        }

        /// <summary>
        /// Cập nhật danh sách vai trò cho một người dùng.
        /// </summary>
        /// <param name="userId">ID của người dùng cần cập nhật vai trò.</param>
        /// <param name="model">Model chứa danh sách các vai trò mới.</param>
        /// <returns>Thông báo thành công hoặc lỗi BadRequest nếu có sự cố.</returns>
        [HttpPut("users/{userId}/roles")]
        [Authorize(Policy = "ManageUserRoles")]
        public async Task<IActionResult> UpdateUserRoles(string userId, [FromBody] UpdateUserRolesModel model)
        {
            var (success, message) = await _adminService.UpdateUserRolesAsync(userId, model);
            if (!success)
            {
                return BadRequest(ApiResponse.Fail(message));
            }
            return Ok(ApiResponse.Success<object>(null, message));
        }
        
        /// <summary>
        /// Lấy danh sách tất cả các vai trò có trong hệ thống.
        /// </summary>
        /// <returns>Danh sách tên các vai trò.</returns>
        [HttpGet("roles")]
        [Authorize(Policy = "ManageUserRoles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return Ok(ApiResponse.Success(roles));
        }

        /// <summary>
        /// Xóa một người dùng khỏi hệ thống.
        /// </summary>
        /// <param name="userId">ID của người dùng cần xóa.</param>
        /// <returns>Thông báo thành công hoặc lỗi BadRequest nếu có sự cố.</returns>
        [HttpDelete("users/{userId}")]
        [Authorize(Policy = "DeleteUsers")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var (success, message) = await _adminService.DeleteUserAsync(userId);
            if (!success)
            {
                return BadRequest(ApiResponse.Fail(message));
            }
            return Ok(ApiResponse.Success<object>(null, message));
        }

        /// <summary>
        /// Lấy danh sách những người dùng có thể được gán vào một nhóm hỗ trợ (thường là vai trò 'Agent').
        /// </summary>
        /// <returns>Danh sách các người dùng có vai trò phù hợp để gán vào nhóm.</returns>
        [HttpGet("assignable-users")]
        [Authorize(Policy = "ManageGroups")]
        public async Task<IActionResult> GetAssignableUsers()
        {
            var assignableRoles = new[] {"Agent"};
            
            var assignableUsers = new List<ApplicationUser>();
            foreach (var role in assignableRoles)
            {
                assignableUsers.AddRange(await _userManager.GetUsersInRoleAsync(role));
            }

            var distinctUsers = assignableUsers.DistinctBy(u => u.Id);
            var userDtos = _mapper.Map<List<UserDto>>(distinctUsers);
            
            return Ok(ApiResponse.Success(userDtos));
        }
    }
} 