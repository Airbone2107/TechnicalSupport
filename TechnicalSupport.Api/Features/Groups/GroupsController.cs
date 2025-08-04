using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechnicalSupport.Api.Common;
using TechnicalSupport.Application.Features.Groups.Abstractions;
using TechnicalSupport.Application.Features.Groups.DTOs;

namespace TechnicalSupport.Api.Features.Groups
{
    [ApiController]
    [Route("[controller]")]
    [Authorize] // Giữ Authorize chung, yêu cầu đăng nhập
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _groupService;

        public GroupsController(IGroupService groupService)
        {
            _groupService = groupService;
        }

        [HttpPost]
        [Authorize(Policy = "ManageGroups")] // Yêu cầu quyền quản lý để tạo
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupModel model)
        {
            var groupDto = await _groupService.CreateGroupAsync(model);
            return CreatedAtAction(nameof(GetMembers), new { groupId = groupDto.GroupId }, ApiResponse.Success(groupDto, "Group created successfully."));
        }

        [HttpGet]
        [Authorize(Policy = "AssignTickets")] // Nới lỏng quyền, chỉ cần quyền gán ticket
        public async Task<IActionResult> GetGroups()
        {
            var groups = await _groupService.GetAllGroupsAsync();
            return Ok(ApiResponse.Success(groups));
        }

        [HttpGet("{groupId}/members")]
        [Authorize(Policy = "ManageGroups")] // Yêu cầu quyền quản lý để xem thành viên
        public async Task<IActionResult> GetMembers(int groupId)
        {
            var members = await _groupService.GetMembersInGroupAsync(groupId);
            return Ok(ApiResponse.Success(members));
        }

        [HttpPost("{groupId}/members")]
        [Authorize(Policy = "ManageGroups")] // Yêu cầu quyền quản lý để thêm thành viên
        public async Task<IActionResult> AddMember(int groupId, [FromBody] AddMemberModel model)
        {
            var (success, message) = await _groupService.AddMemberToGroupAsync(groupId, model);
            if (!success)
            {
                return BadRequest(ApiResponse.Fail(message));
            }
            return Ok(ApiResponse.Success<object>(null, message));
        }

        [HttpDelete("{groupId}/members/{userId}")]
        [Authorize(Policy = "ManageGroups")] // Yêu cầu quyền quản lý để xóa thành viên
        public async Task<IActionResult> RemoveMember(int groupId, string userId)
        {
            var (success, message) = await _groupService.RemoveMemberFromGroupAsync(groupId, userId);
            if (!success)
            {
                return BadRequest(ApiResponse.Fail(message));
            }
            return Ok(ApiResponse.Success<object>(null, message));
        }
    }
} 