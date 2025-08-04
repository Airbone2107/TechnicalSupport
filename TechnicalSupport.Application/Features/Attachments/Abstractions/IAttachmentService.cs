using TechnicalSupport.Application.Features.Attachments.DTOs;

namespace TechnicalSupport.Application.Features.Attachments.Abstractions
{
    public interface IAttachmentService
    {
        Task<List<AttachmentDto>> UploadAttachmentsForTicketAsync(int ticketId, IEnumerable<FileContentDto> files);
        Task<List<AttachmentDto>> GetAttachmentsForTicketAsync(int ticketId);
        Task<FileDownloadDto?> GetAttachmentFileAsync(int attachmentId);
        Task<bool> DeleteAttachmentAsync(int attachmentId);
    }
} 