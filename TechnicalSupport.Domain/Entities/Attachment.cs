using System.ComponentModel.DataAnnotations;

namespace TechnicalSupport.Domain.Entities
{
    public class Attachment
    {
        public int AttachmentId { get; set; }

        [Required]
        public int TicketId { get; set; }
        public Ticket Ticket { get; set; }

        public int? CommentId { get; set; }
        public Comment? Comment { get; set; }

        [Required]
        public string UploadedById { get; set; }
        public ApplicationUser UploadedBy { get; set; }

        [Required, StringLength(255)]
        public string OriginalFileName { get; set; }

        [Required, StringLength(255)]
        public string StoredPath { get; set; }

        [StringLength(100)]
        public string? FileType { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
} 