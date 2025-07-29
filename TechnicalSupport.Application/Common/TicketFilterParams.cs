using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechnicalSupport.Application.Common
{
    public class TicketFilterParams : PaginationParams
    {
        /// <summary>
        /// Lọc theo ID của trạng thái (StatusId).
        /// </summary>
        public int? StatusId { get; set; }

        /// <summary>
        /// Lọc theo độ ưu tiên (ví dụ: "High", "Medium", "Low").
        /// </summary>
        public string? Priority { get; set; }

        /// <summary>
        /// Lọc theo ID của technician được gán.
        /// </summary>
        public string? AssigneeId { get; set; }

        /// <summary>
        /// Tìm kiếm từ khóa trong Title và Description của ticket.
        /// </summary>
        public string? SearchQuery { get; set; }

    }
}
