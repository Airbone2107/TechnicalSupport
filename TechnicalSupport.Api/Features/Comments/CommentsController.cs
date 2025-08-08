using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechnicalSupport.Api.Common;
using TechnicalSupport.Application.Features.Comments.Abstractions;
using TechnicalSupport.Application.Features.Tickets.DTOs;

namespace TechnicalSupport.Api.Features.Comments
{
    /// <summary>
    /// Cung cấp các endpoint để quản lý các bình luận riêng lẻ.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Authorize(Policy = "RequireAuthenticatedUser")]
    public class CommentsController : ControllerBase
    {
        private readonly ICommentService _commentService;

        /// <summary>
        /// Khởi tạo một instance mới của CommentsController.
        /// </summary>
        public CommentsController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một bình luận theo ID.
        /// </summary>
        /// <param name="id">ID của bình luận.</param>
        /// <returns>Thông tin chi tiết của bình luận.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetComment(int id)
        {
            var commentDto = await _commentService.GetCommentByIdAsync(id);
            if (commentDto == null)
            {
                return NotFound(ApiResponse.Fail($"Comment with Id {id} not found."));
            }
            return Ok(ApiResponse.Success(commentDto));
        }

        /// <summary>
        /// Cập nhật nội dung của một bình luận.
        /// </summary>
        /// <param name="id">ID của bình luận cần cập nhật.</param>
        /// <param name="model">Dữ liệu mới cho bình luận.</param>
        /// <returns>Thông tin bình luận sau khi đã cập nhật.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComment(int id, [FromBody] UpdateCommentModel model)
        {
            try
            {
                var commentDto = await _commentService.UpdateCommentAsync(id, model);
                if (commentDto == null)
                {
                    return NotFound(ApiResponse.Fail($"Comment with Id {id} not found."));
                }
                return Ok(ApiResponse.Success(commentDto, "Comment updated successfully."));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail(ex.Message));
            }
        }

        /// <summary>
        /// Xóa một bình luận.
        /// </summary>
        /// <param name="id">ID của bình luận cần xóa.</param>
        /// <returns>Thông báo thành công.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            try
            {
                var success = await _commentService.DeleteCommentAsync(id);
                if (!success)
                {
                    return NotFound(ApiResponse.Fail($"Comment with Id {id} not found."));
                }
                return Ok(ApiResponse.Success<object>(null, "Comment deleted successfully."));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail(ex.Message));
            }
        }
    }
} 