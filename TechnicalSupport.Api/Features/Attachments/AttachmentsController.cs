using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechnicalSupport.Api.Common;
using TechnicalSupport.Application.Features.Attachments.Abstractions;
using TechnicalSupport.Application.Features.Attachments.DTOs;

namespace TechnicalSupport.Api.Features.Attachments
{
    [ApiController]
    [Authorize(Policy = "RequireAuthenticatedUser")]
    public class AttachmentsController : ControllerBase
    {
        private readonly IAttachmentService _attachmentService;

        public AttachmentsController(IAttachmentService attachmentService)
        {
            _attachmentService = attachmentService;
        }

        [HttpPost("tickets/{ticketId}/attachments")]
        [ProducesResponseType(typeof(ApiResponse<List<AttachmentDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> UploadAttachments(int ticketId, IFormFileCollection files)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(ApiResponse.Fail("No files were uploaded."));
            }

            var fileContents = new List<FileContentDto>();
            foreach (var file in files)
            {
                fileContents.Add(new FileContentDto
                {
                    Content = file.OpenReadStream(),
                    FileName = file.FileName,
                    ContentType = file.ContentType
                });
            }

            try
            {
                var result = await _attachmentService.UploadAttachmentsForTicketAsync(ticketId, fileContents);
                return Ok(ApiResponse.Success(result, "Files uploaded successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse.Fail(ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception)
            {
                // Nên ghi log lỗi ở đây
                return StatusCode(500, ApiResponse.Fail("An internal error occurred while uploading files."));
            }
        }

        [HttpGet("tickets/{ticketId}/attachments")]
        public async Task<IActionResult> GetAttachmentsForTicket(int ticketId)
        {
            try
            {
                var attachments = await _attachmentService.GetAttachmentsForTicketAsync(ticketId);
                return Ok(ApiResponse.Success(attachments));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse.Fail(ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("attachments/{attachmentId}")]
        public async Task<IActionResult> DownloadAttachment(int attachmentId)
        {
            try
            {
                var result = await _attachmentService.GetAttachmentFileAsync(attachmentId);
                if (result == null)
                {
                    return NotFound(ApiResponse.Fail("Attachment not found or file is missing."));
                }

                return File(result.Content, result.ContentType, result.FileName);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpDelete("attachments/{attachmentId}")]
        public async Task<IActionResult> DeleteAttachment(int attachmentId)
        {
            try
            {
                var success = await _attachmentService.DeleteAttachmentAsync(attachmentId);
                if (!success)
                {
                    return NotFound(ApiResponse.Fail("Attachment not found."));
                }
                return Ok(ApiResponse.Success<object>(null, "Attachment deleted successfully."));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
} 