namespace TechnicalSupport.Application.Features.Attachments.DTOs
{
    public class FileDownloadDto
    {
        public Stream Content { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
    }
} 