using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechnicalSupport.Application.Features.Attachments.DTOs
{
    public class FileDownloadDto
    {
        public byte[] FileContents { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
    }
}
