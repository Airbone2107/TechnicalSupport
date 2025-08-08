using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechnicalSupport.Api.Common;
using TechnicalSupport.Application.Features.Groups.Abstractions;
using TechnicalSupport.Application.Features.Groups.DTOs;

namespace TechnicalSupport.Api.Features.Groups
{
    /// <summary>
    /// Quản lý các nhóm hỗ trợ kỹ thuật.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _groupService;

        /// <summary>
        /// Khởi tạo một instance mới của GroupsController.
        /// </summary>
        public GroupsController(IGroupService groupService)
        {
            _groupService = groupService;
        }

        /// <summary>
        /// Tạo một nhóm hỗ trợ mới.
        /// </summary>
        /// <param name="model">Thông tin của nhóm mới.</param>
        /// <returns>Thông tin nhóm vừa được tạo.</returns>
        [HttpPost]
        [Authorize(Policy = "ManageGroups")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupModel model)
        {
            var groupDto = await _groupService.CreateGroupAsync(model);
            return CreatedAtAction(nameof(GetMembers), new { groupId = groupDto.GroupId }, ApiResponse.Success(groupDto, "Group created successfully."));
        }

        /// <summary>
        /// Lấy danh sách tất cả các nhóm hỗ trợ.
        /// </summary>
        /// <returns>Danh sách các nhóm.</returns>
        [HttpGet]
        [Authorize(Policy = "ViewGroups")]
        public async Task<IActionResult> GetGroups()
        {
            var groups = await _groupService.GetAllGroupsAsync();
            return Ok(ApiResponse.Success(groups));
        }

        /// <summary>
        /// Lấy danh sách các thành viên trong một nhóm cụ thể.
        /// </summary>
        /// <param name="groupId">ID của nhóm.</param>
        /// <returns>Danh sách các người dùng là thành viên của nhóm.</returns>
        [HttpGet("{groupId}/members")]
        [Authorize(Policy = "ReadGroupMembers")]
        public async Task<IActionResult> GetMembers(int groupId)
        {
            try
            {
                var members = await _groupService.GetMembersInGroupAsync(groupId);
                return Ok(ApiResponse.Success(members));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse.Fail(ex.Message));
            }
        }

        /// <summary>
        /// Thêm một thành viên vào nhóm.
        /// </summary>
        /// <param name="groupId">ID của nhóm.</param>
        /// <param name="model">Model chứa ID của người dùng cần thêm.</param>
        /// <returns>Thông báo thành công hoặc lỗi.</returns>
        [HttpPost("{groupId}/members")]
        [Authorize(Policy = "ManageGroups")]
        public async Task<IActionResult> AddMember(int groupId, [FromBody] AddMemberModel model)
        {
            var (success, message) = await _groupService.AddMemberToGroupAsync(groupId, model);
            if (!success)
            {
                return BadRequest(ApiResponse.Fail(message));
            }
            return Ok(ApiResponse.Success<object>(null, message));
        }

        /// <summary>
        /// Xóa một thành viên khỏi nhóm.
        /// </summary>
        /// <param name="groupId">ID của nhóm.</param>
        /// <param name="userId">ID của người dùng cần xóa.</param>
        /// <returns>Thông báo thành công hoặc lỗi.</returns>
        [HttpDelete("{groupId}/members/{userId}")]
        [Authorize(Policy = "ManageGroups")]
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