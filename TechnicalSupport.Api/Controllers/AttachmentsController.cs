using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TechnicalSupport.Api.Common;
using TechnicalSupport.Application.Features.Attachments.DTOs;
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

        [HttpPost("api/tickets/{ticketId}/attachments")]
        [ProducesResponseType(typeof(ApiResponse<List<AttachmentDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> UploadAttachments(int ticketId, IFormFileCollection files)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(ApiResponse.Fail("No files were uploaded."));
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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
                var result = await _attachmentService.UploadAttachmentsForTicketAsync(ticketId, userId!, fileContents);
                return Ok(ApiResponse.Success(result, "Files uploaded successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse.Fail(ex.Message));
            }
            catch (UnauthorizedAccessException) // SỬA CẢNH BÁO: Bỏ biến 'ex' không sử dụng
            {
                return Forbid();
            }
            catch (Exception) // SỬA CẢNH BÁO: Bỏ biến 'ex' không sử dụng
            {
                // Nên ghi log lỗi ở đây
                return StatusCode(500, ApiResponse.Fail("An internal error occurred while uploading files."));
            }
        }

        [HttpGet("api/tickets/{ticketId}/attachments")]
        public async Task<IActionResult> GetAttachmentsForTicket(int ticketId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                var attachments = await _attachmentService.GetAttachmentsForTicketAsync(ticketId, currentUserId!);
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

        [HttpGet("api/attachments/{attachmentId}")]
        public async Task<IActionResult> DownloadAttachment(int attachmentId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                var result = await _attachmentService.GetAttachmentFileAsync(attachmentId, currentUserId!);
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

        [HttpDelete("api/attachments/{attachmentId}")]
        public async Task<IActionResult> DeleteAttachment(int attachmentId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                var success = await _attachmentService.DeleteAttachmentAsync(attachmentId, currentUserId!);
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