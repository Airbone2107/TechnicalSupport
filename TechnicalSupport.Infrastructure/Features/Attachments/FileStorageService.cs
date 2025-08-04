using Microsoft.AspNetCore.Hosting;
using TechnicalSupport.Application.Configurations;
using TechnicalSupport.Application.Features.Attachments.Abstractions;
using TechnicalSupport.Application.Features.Attachments.DTOs;

namespace TechnicalSupport.Infrastructure.Features.Attachments
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string _baseStoragePath;
        private readonly IWebHostEnvironment _env;

        public FileStorageService(IWebHostEnvironment env, AttachmentSettings attachmentSettings)
        {
            _env = env;
            var webRootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
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

            var relativePath = Path.Combine(Path.GetFileName(_baseStoragePath), subDirectory, uniqueFileName);
            return relativePath.Replace('\\', '/');
        }
        
        public Task<(Stream Content, string ContentType)?> GetFileAsync(string storedPath)
        {
            var webRootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var physicalPath = Path.Combine(webRootPath, storedPath.Replace('/', Path.DirectorySeparatorChar));
            
            if (!File.Exists(physicalPath))
            {
                 return Task.FromResult<(Stream, string)?>(null);
            }

            var stream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var contentType = "application/octet-stream";
            
            return Task.FromResult<(Stream, string)?>((stream, contentType));
        }

        public void DeleteFile(string storedPath)
        {
            if (string.IsNullOrEmpty(storedPath)) return;
            
            var webRootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var physicalPath = Path.Combine(webRootPath, storedPath.Replace('/', Path.DirectorySeparatorChar));
            
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
        }
    }
} 