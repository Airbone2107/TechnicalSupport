using TechnicalSupport.Application.Features.Attachments.DTOs;

namespace TechnicalSupport.Application.Interfaces
{
    public interface IAttachmentService
    {
        Task<List<AttachmentDto>> UploadAttachmentsForTicketAsync(int ticketId, string userId, IEnumerable<FileContentDto> files);
        Task<List<AttachmentDto>> GetAttachmentsForTicketAsync(int ticketId, string currentUserId);
        Task<FileDownloadDto?> GetAttachmentFileAsync(int attachmentId, string currentUserId);
        Task<bool> DeleteAttachmentAsync(int attachmentId, string currentUserId);
    }
} 