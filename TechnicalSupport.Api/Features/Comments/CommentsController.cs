using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechnicalSupport.Api.Common;
using TechnicalSupport.Application.Features.Comments.Abstractions;
using TechnicalSupport.Application.Features.Tickets.DTOs;

namespace TechnicalSupport.Api.Features.Comments
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Policy = "RequireAuthenticatedUser")]
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