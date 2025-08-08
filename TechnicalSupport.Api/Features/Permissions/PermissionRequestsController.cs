using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechnicalSupport.Api.Common;
using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Features.Permissions.Abstractions;
using TechnicalSupport.Application.Features.Permissions.DTOs;

namespace TechnicalSupport.Api.Features.Permissions
{
    /// <summary>
    /// Quản lý các yêu cầu cấp quyền từ người dùng.
    /// </summary>
    [ApiController]
    [Route("permission-requests")]
    [Authorize]
    public class PermissionRequestsController : ControllerBase
    {
        private readonly IPermissionRequestService _permissionRequestService;

        /// <summary>
        /// Khởi tạo một instance mới của PermissionRequestsController.
        /// </summary>
        public PermissionRequestsController(IPermissionRequestService permissionRequestService)
        {
            _permissionRequestService = permissionRequestService;
        }

        /// <summary>
        /// Tạo một yêu cầu cấp quyền mới.
        /// </summary>
        /// <param name="model">Thông tin yêu cầu cấp quyền.</param>
        /// <returns>Thông tin chi tiết của yêu cầu vừa tạo.</returns>
        [HttpPost]
        [Authorize(Policy = "RequestPermissions")]
        public async Task<IActionResult> CreateRequest([FromBody] CreatePermissionRequestModel model)
        {
            var result = await _permissionRequestService.CreateRequestAsync(model);
            return Ok(ApiResponse.Success(result, "Permission request created successfully."));
        }

        /// <summary>
        /// Lấy danh sách các yêu cầu cấp quyền.
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang.</param>
        /// <param name="pendingOnly">Chỉ lấy các yêu cầu đang chờ xử lý.</param>
        /// <returns>Danh sách các yêu cầu cấp quyền đã phân trang.</returns>
        [HttpGet]
        [Authorize(Policy = "ReviewPermissions")]
        public async Task<IActionResult> GetRequests([FromQuery] PaginationParams paginationParams, [FromQuery] bool pendingOnly = true)
        {
            var result = await _permissionRequestService.GetRequestsAsync(paginationParams, pendingOnly);
            return Ok(ApiResponse.Success(result));
        }

        /// <summary>
        /// Phê duyệt một yêu cầu cấp quyền.
        /// </summary>
        /// <param name="id">ID của yêu cầu.</param>
        /// <param name="model">Ghi chú của người xử lý.</param>
        /// <returns>Thông báo thành công hoặc lỗi.</returns>
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

        /// <summary>
        /// Từ chối một yêu cầu cấp quyền.
        /// </summary>
        /// <param name="id">ID của yêu cầu.</param>
        /// <param name="model">Ghi chú của người xử lý.</param>
        /// <returns>Thông báo thành công hoặc lỗi.</returns>
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