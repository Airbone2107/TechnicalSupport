namespace TechnicalSupport.Application.Common
{
    public class TicketFilterParams : PaginationParams
    {
        /// <summary>
        /// Danh sách các tên trạng thái để lọc (thay thế cho StatusIds).
        /// Ví dụ: ["Open", "InProgress"]
        /// </summary>
        public List<string>? Statuses { get; set; }

        public string? Priority { get; set; }
        public string? SearchQuery { get; set; }
        public bool? UnassignedToGroupOnly { get; set; }
        public bool? CreatedByMe { get; set; }

        /// <summary>
        /// Lọc các ticket được gán cho chính người dùng đang đăng nhập.
        /// </summary>
        public bool? MyTicket { get; set; }

        /// <summary>
        /// Lọc các ticket thuộc các nhóm mà người dùng đang đăng nhập là thành viên.
        /// </summary>
        public bool? MyGroupTicket { get; set; }
    }
}