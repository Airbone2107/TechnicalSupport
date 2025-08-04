using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechnicalSupport.Api.Common;
using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Features.Permissions.Abstractions;
using TechnicalSupport.Application.Features.Permissions.DTOs;

namespace TechnicalSupport.Api.Features.Permissions
{
    [ApiController]
    [Route("permission-requests")]
    [Authorize]
    public class PermissionRequestsController : ControllerBase
    {
        private readonly IPermissionRequestService _permissionRequestService;

        public PermissionRequestsController(IPermissionRequestService permissionRequestService)
        {
            _permissionRequestService = permissionRequestService;
        }

        [HttpPost]
        [Authorize(Policy = "RequestPermissions")]
        public async Task<IActionResult> CreateRequest([FromBody] CreatePermissionRequestModel model)
        {
            var result = await _permissionRequestService.CreateRequestAsync(model);
            return Ok(ApiResponse.Success(result, "Permission request created successfully."));
        }

        [HttpGet]
        [Authorize(Policy = "ReviewPermissions")]
        public async Task<IActionResult> GetRequests([FromQuery] PaginationParams paginationParams, [FromQuery] bool pendingOnly = true)
        {
            var result = await _permissionRequestService.GetRequestsAsync(paginationParams, pendingOnly);
            return Ok(ApiResponse.Success(result));
        }

        [HttpPut("{id}/approve")]
        [Authorize(Policy = "ReviewPermissions")]
        public async Task<IActionResult> ApproveRequest(int id, [FromBody] ProcessPermissionRequestModel model)
        {
            var (success, message) = await _permissionRequestService.ApproveRequestAsync(id, model);
            if (!success)
            {
                return BadRequest(ApiResponse.Fail(message));
            }
            return Ok(ApiResponse.Success<object>(null, message));
        }

        [HttpPut("{id}/reject")]
        [Authorize(Policy = "ReviewPermissions")]
        public async Task<IActionResult> RejectRequest(int id, [FromBody] ProcessPermissionRequestModel model)
        {
            var (success, message) = await _permissionRequestService.RejectRequestAsync(id, model);
            if (!success)
            {
                return BadRequest(ApiResponse.Fail(message));
            }
            return Ok(ApiResponse.Success<object>(null, message));
        }
    }
} 