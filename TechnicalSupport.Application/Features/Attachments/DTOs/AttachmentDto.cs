using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechnicalSupport.Application.Features.Attachments.DTOs
{
    public class AttachmentDto
    {
        public int AttachmentId { get; set; }
        public string OriginalFileName { get; set; } = string.Empty; // <--- SỬA DÒNG NÀY
        public string? FileType { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedByDisplayName { get; set; } = string.Empty; // <--- SỬA DÒNG NÀY
    }
}
