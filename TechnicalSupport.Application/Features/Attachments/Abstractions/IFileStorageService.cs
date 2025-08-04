using TechnicalSupport.Application.Features.Attachments.DTOs;

namespace TechnicalSupport.Application.Features.Attachments.Abstractions
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Lưu file vào nơi lưu trữ và trả về đường dẫn tương đối.
        /// </summary>
        /// <param name="file">Nội dung file.</param>
        /// <param name="subDirectory">Thư mục con để tổ chức file (ví dụ: theo Ticket ID).</param>
        /// <returns>Đường dẫn tương đối đến file đã lưu.</returns>
        Task<string> SaveFileAsync(FileContentDto file, string subDirectory);
        
        /// <summary>
        /// Lấy nội dung của một file từ nơi lưu trữ.
        /// </summary>
        /// <param name="storedPath">Đường dẫn tương đối đã lưu trong DB.</param>
        /// <returns>Một tuple chứa Stream nội dung và content type, hoặc null nếu không tìm thấy.</returns>
        Task<(Stream Content, string ContentType)?> GetFileAsync(string storedPath);

        /// <summary>
        /// Xóa một file khỏi nơi lưu trữ.
        /// </summary>
        /// <param name="storedPath">Đường dẫn tương đối đã lưu trong DB.</param>
        void DeleteFile(string storedPath);
    }
} 