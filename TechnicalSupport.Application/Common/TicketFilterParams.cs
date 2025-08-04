namespace TechnicalSupport.Application.Common
{
    public class TicketFilterParams : PaginationParams
    {
        /// <summary>
        /// Danh sách các ID trạng thái để lọc. Thay thế cho StatusId.
        /// </summary>
        public List<int>? StatusIds { get; set; }
        
        public string? Priority { get; set; }
        public string? AssigneeId { get; set; }
        public string? SearchQuery { get; set; }
        public bool? UnassignedToGroupOnly { get; set; }
        public bool? CreatedByMe { get; set; }

        /// <summary>
        /// Nếu true, chỉ trả về các ticket thuộc các nhóm mà người dùng hiện tại là thành viên.
        /// </summary>
        public bool? TicketForMyGroup { get; set; }

        // Thuộc tính `StatusId` không còn được sử dụng và đã được loại bỏ.
    }
}