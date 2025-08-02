namespace TechnicalSupport.Application.Features.Attachments.DTOs
{
    public class FileContentDto
    {
        public Stream Content { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
    }
} 