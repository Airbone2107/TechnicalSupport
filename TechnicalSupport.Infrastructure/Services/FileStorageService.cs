using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Threading.Tasks;
using TechnicalSupport.Application.Configurations; // Sửa đổi ở đây
using TechnicalSupport.Application.Features.Attachments.DTOs;
using TechnicalSupport.Application.Interfaces;

namespace TechnicalSupport.Infrastructure.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string _baseStoragePath;

        public FileStorageService(IWebHostEnvironment env, AttachmentSettings attachmentSettings)
        {
            // Sử dụng ContentRootPath và wwwroot để đảm bảo đường dẫn chính xác
            // _baseStoragePath sẽ là đường dẫn vật lý tuyệt đối, ví dụ: "C:\project\TechnicalSupport.Api\wwwroot\Attachments"
            var webRootPath = Path.Combine(env.ContentRootPath, "wwwroot");
            _baseStoragePath = Path.Combine(webRootPath, attachmentSettings.StoragePath);

            if (!Directory.Exists(_baseStoragePath))
            {
                Directory.CreateDirectory(_baseStoragePath);
            }
        }

        public async Task<string> SaveFileAsync(FileContentDto file, string subDirectory)
        {
            var targetDirectory = Path.Combine(_baseStoragePath, subDirectory);
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(targetDirectory, uniqueFileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.Content.CopyToAsync(stream);
            }

            // Trả về đường dẫn tương đối để lưu vào DB, ví dụ: "Attachments/1/guid_filename.txt"
            // Đường dẫn này được xây dựng từ cấu hình ban đầu, không phải đường dẫn vật lý.
            var relativePath = Path.Combine(Path.GetFileName(_baseStoragePath), subDirectory, uniqueFileName);
            return relativePath.Replace('\\', '/');
        }
        
        public Task<(Stream Content, string ContentType)?> GetFileAsync(string storedPath)
        {
            // Chuyển đổi đường dẫn tương đối thành đường dẫn vật lý
            var physicalPath = Path.Combine(Path.GetDirectoryName(_baseStoragePath)!, storedPath.Replace('/', Path.DirectorySeparatorChar));
            
            if (!File.Exists(physicalPath))
            {
                // Thử một cách khác nếu cấu trúc thư mục khác
                var alternativePath = Path.Combine(_baseStoragePath, storedPath.Split(new[] { '/' }, 2).Last().Replace('/', Path.DirectorySeparatorChar));
                 if (!File.Exists(alternativePath))
                 {
                    return Task.FromResult<(Stream, string)?>(null);
                 }
                 physicalPath = alternativePath;
            }

            var stream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var contentType = "application/octet-stream";
            
            return Task.FromResult<(Stream, string)?>((stream, contentType));
        }

        public void DeleteFile(string storedPath)
        {
            if (string.IsNullOrEmpty(storedPath)) return;

            var physicalPath = Path.Combine(Path.GetDirectoryName(_baseStoragePath)!, storedPath.Replace('/', Path.DirectorySeparatorChar));
            
             if (!File.Exists(physicalPath))
            {
                var alternativePath = Path.Combine(_baseStoragePath, storedPath.Split(new[] { '/' }, 2).Last().Replace('/', Path.DirectorySeparatorChar));
                 if (File.Exists(alternativePath))
                 {
                    physicalPath = alternativePath;
                 } else {
                    return; // File không tồn tại
                 }
            }
            
            File.Delete(physicalPath);
        }
    }
} 