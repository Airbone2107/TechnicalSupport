using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechnicalSupport.Api.Common;
using TechnicalSupport.Application.Features.Tickets.DTOs;
using TechnicalSupport.Application.Interfaces;

namespace TechnicalSupport.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CommentsController : ControllerBase
    {
        private readonly ICommentService _commentService;

        public CommentsController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetComment(int id)
        {
            // Logic kiểm tra quyền truy cập ticket chứa comment này nên được thêm vào service
            // Tạm thời, giả định nếu user được xác thực thì có thể xem comment
            var commentDto = await _commentService.GetCommentByIdAsync(id);
            if (commentDto == null)
            {
                return NotFound(ApiResponse.Fail($"Comment with Id {id} not found."));
            }
            return Ok(ApiResponse.Success(commentDto));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComment(int id, [FromBody] UpdateCommentModel model)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                var commentDto = await _commentService.UpdateCommentAsync(id, model, currentUserId!);
                if (commentDto == null)
                {
                    return NotFound(ApiResponse.Fail($"Comment with Id {id} not found."));
                }
                return Ok(ApiResponse.Success(commentDto, "Comment updated successfully."));
            }
            catch (UnauthorizedAccessException ex)
            {
                // SỬA LỖI: Phương thức Forbid() không chấp nhận tham số body.
                // Sử dụng StatusCode(403, body) để trả về lỗi Forbidden với nội dung tùy chỉnh.
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            try
            {
                var success = await _commentService.DeleteCommentAsync(id, currentUserId!, isAdmin);
                if (!success)
                {
                    return NotFound(ApiResponse.Fail($"Comment with Id {id} not found."));
                }
                return Ok(ApiResponse.Success<object>(null, "Comment deleted successfully."));
            }
            catch (UnauthorizedAccessException ex)
            {
                // SỬA LỖI: Tương tự như trên, sử dụng StatusCode để trả về lỗi 403 với body.
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail(ex.Message));
            }
        }
    }
} 