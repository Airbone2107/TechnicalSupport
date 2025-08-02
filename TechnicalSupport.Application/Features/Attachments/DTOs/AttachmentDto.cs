namespace TechnicalSupport.Application.Features.Attachments.DTOs
{
    public class AttachmentDto
    {
        public int AttachmentId { get; set; }
        public int TicketId { get; set; }
        public string OriginalFileName { get; set; }
        public string StoredPath { get; set; }
        public string FileType { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedByDisplayName { get; set; }
    }
} 