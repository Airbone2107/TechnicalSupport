// TechnicalSupport.Api/Controllers/AttachmentsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechnicalSupport.Api.Common;
using TechnicalSupport.Application.Interfaces;

namespace TechnicalSupport.Api.Controllers
{
    [ApiController]
    [Authorize]
    public class AttachmentsController : ControllerBase
    {
        private readonly IAttachmentService _attachmentService;

        public AttachmentsController(IAttachmentService attachmentService)
        {
            _attachmentService = attachmentService;
        }

        [HttpGet("tickets/{ticketId}/attachments")]
        public async Task<IActionResult> GetAttachments(int ticketId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var attachments = await _attachmentService.GetAttachmentsForTicketAsync(ticketId, userId);
                return Ok(ApiResponse.Success(attachments));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse.Fail(ex.Message));
            }
            catch (System.Security.Authentication.AuthenticationException ex)
            {
                return Forbid(ex.Message);
            }
        }

        [HttpPost("tickets/{ticketId}/attachments")]
        public async Task<IActionResult> UploadAttachments(int ticketId, List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(ApiResponse.Fail("No files were uploaded."));
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var attachments = await _attachmentService.UploadAttachmentsAsync(ticketId, files, userId);
                return Ok(ApiResponse.Success(attachments, $"{attachments.Count} file(s) uploaded successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse.Fail(ex.Message));
            }
            catch (System.Security.Authentication.AuthenticationException ex)
            {
                return Forbid(ex.Message);
            }
        }

        // Thêm 2 method này vào class AttachmentsController

        [HttpGet("attachments/{attachmentId}")]
        public async Task<IActionResult> DownloadAttachment(int attachmentId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var fileDto = await _attachmentService.GetAttachmentForDownloadAsync(attachmentId, userId);

                if (fileDto == null)
                {
                    return NotFound(ApiResponse.Fail("Attachment not found."));
                }

                return File(fileDto.FileContents, fileDto.ContentType, fileDto.FileName);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse.Fail(ex.Message));
            }
            catch (System.Security.Authentication.AuthenticationException ex)
            {
                return Forbid(); // Trả về 403 Forbidden mà không cần message
            }
        }

        [HttpDelete("attachments/{attachmentId}")]
        public async Task<IActionResult> DeleteAttachment(int attachmentId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var success = await _attachmentService.DeleteAttachmentAsync(attachmentId, userId);

                if (!success)
                {
                    return NotFound(ApiResponse.Fail("Attachment not found."));
                }

                return Ok(ApiResponse.Success<object>(null, "Attachment deleted successfully."));
            }
            catch (System.Security.Authentication.AuthenticationException ex)
            {
                return Forbid(); // Trả về 403 Forbidden
            }
        }
    }
}