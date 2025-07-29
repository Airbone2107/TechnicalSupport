using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechnicalSupport.Api.Common;
using TechnicalSupport.Application.Features.Groups.DTOs;
using TechnicalSupport.Application.Interfaces;

namespace TechnicalSupport.Api.Controllers
{
   
    [ApiController]
    [Route("groups")]
    [Authorize(Roles = "Admin")]
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _groupService;

        public GroupsController(IGroupService groupService)
        {
            _groupService = groupService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupModel model)
        {
            var group = await _groupService.CreateGroupAsync(model);
            return CreatedAtAction(nameof(CreateGroup), new { id = group.GroupId }, ApiResponse.Success(group));
        }

        [HttpGet]
        public async Task<IActionResult> GetGroups()
        {
            var groups = await _groupService.GetGroupsAsync();
            return Ok(ApiResponse.Success(groups));
        }

        [HttpPost("{groupId}/members")]
        public async Task<IActionResult> AddMember(int groupId, [FromBody] AddMemberModel model)
        {
            var success = await _groupService.AddMemberToGroupAsync(groupId, model.UserId);
            if (!success)
            {
                return BadRequest(ApiResponse.Fail("Failed to add member. Group or technician not found."));
            }
            return Ok(ApiResponse.Success<object>(null, "Member added successfully."));
        }

        [HttpDelete("{groupId}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(int groupId, string userId)
        {
            var success = await _groupService.RemoveMemberFromGroupAsync(groupId, userId);
            if (!success)
            {
                return NotFound(ApiResponse.Fail("Membership not found."));
            }
            return Ok(ApiResponse.Success<object>(null, "Member removed successfully."));
        }
    }
}
