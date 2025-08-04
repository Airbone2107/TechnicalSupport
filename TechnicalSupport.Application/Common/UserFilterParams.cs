namespace TechnicalSupport.Application.Common
{
    public class UserFilterParams : PaginationParams
    {
        /// <summary>
        /// Lọc người dùng theo tên vai trò (ví dụ: "Client", "Agent").
        /// </summary>
        public string? Role { get; set; }

        /// <summary>
        /// Chuỗi tìm kiếm theo tên hiển thị (DisplayName).
        /// </summary>
        public string? DisplayNameQuery { get; set; }
    }
} 