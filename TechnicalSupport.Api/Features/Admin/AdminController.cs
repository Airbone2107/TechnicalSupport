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
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IMapper _mapper;

        public AdminController(IAdminService adminService, UserManager<ApplicationUser> userManager, IMapper mapper, RoleManager<IdentityRole> roleManager)
        {
            _adminService = adminService;
            _userManager = userManager;
            _mapper = mapper;
            _roleManager = roleManager;
        }

        [HttpGet("users")]
        [Authorize(Policy = "ReadUsers")]
        public async Task<IActionResult> GetUsers([FromQuery] UserFilterParams filterParams)
        {
            var users = await _adminService.GetUsersAsync(filterParams);
            return Ok(ApiResponse.Success(users));
        }

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
        
        [HttpGet("roles")]
        [Authorize(Policy = "ManageUserRoles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return Ok(ApiResponse.Success(roles));
        }

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

        [HttpGet("assignable-users")]
        [Authorize(Policy = "AssignTickets")]
        public async Task<IActionResult> GetAssignableUsers()
        {
            var agents = await _userManager.GetUsersInRoleAsync("Agent");
            var managers = await _userManager.GetUsersInRoleAsync("Manager");

            var assignableUsers = agents.Union(managers).DistinctBy(u => u.Id);

            var userDtos = _mapper.Map<List<UserDto>>(assignableUsers);
            
            return Ok(ApiResponse.Success(userDtos));
        }
    }
} 