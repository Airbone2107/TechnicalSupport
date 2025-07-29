using Microsoft.AspNetCore.Http;
using TechnicalSupport.Application.Features.Attachments.DTOs;

namespace TechnicalSupport.Application.Interfaces
{
    public interface IAttachmentService
    {
        Task<List<AttachmentDto>> UploadAttachmentsAsync(int ticketId, List<IFormFile> files, string userId);
        Task<List<AttachmentDto>> GetAttachmentsForTicketAsync(int ticketId, string userId);
        Task<FileDownloadDto?> GetAttachmentForDownloadAsync(int attachmentId, string userId);
        Task<bool> DeleteAttachmentAsync(int attachmentId, string userId);
    }
}
